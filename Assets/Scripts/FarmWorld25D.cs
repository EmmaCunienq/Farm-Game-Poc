using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class FarmWorld25D : IDisposable
{
    readonly FarmArt art;
    readonly FarmSimulation state;
    readonly Transform root;
    readonly List<Transform> cards=new();
    readonly List<Material> materials=new();
    readonly List<Mesh> meshes=new();
    readonly Dictionary<Texture,Material> paperMaterials=new();
    readonly Dictionary<Sprite,Mesh> spriteMeshes=new();
    readonly Dictionary<Vector2Int,GameObject> plots=new();
    readonly List<Light> lanterns=new();
    readonly MaterialPropertyBlock tint=new();
    Transform actor, head, torso, leftLeg, rightLeg, focus;
    MeshRenderer targetRenderer, house, playerShadow;
    Material contactMaterial;
    const float CameraPitch=35f, CameraDistance=17f;
    readonly List<MeshRenderer> actorRenderers=new();
    Light sun,moon;
    CharacterController controller;
    float yaw, actionUntil;
    public Camera Camera { get; private set; }
    public Transform Player { get; private set; }
    public Vector2 Facing { get; private set; }=Vector2.up;
    public float LastMoveMagnitude { get; private set; }
    public FarmSimulation State=>state;
    public bool NearShop=>Vector2.Distance(new Vector2(Player.position.x,Player.position.z),new Vector2(11.8f,7.2f))<3.2f;
    public static Vector3 Position(int x,int z)=>new Vector3(x,GroundHeight(x,z)+.025f,z);
    public static float GroundHeight(float x,float z)
    {
        float edge=Mathf.Max(0,Mathf.Max(Mathf.Max(-x,x-15),Mathf.Max(-z,z-11)));
        return Mathf.SmoothStep(0,1,Mathf.Clamp01(edge/3f))*(.5f+.4f*Mathf.Sin(x*.45f)*Mathf.Cos(z*.4f));
    }

    public FarmWorld25D(FarmArt art,FarmSimulation state,Transform owner)
    {
        this.art=art;this.state=state;
        root=new GameObject("Monde 3D — décors en papier").transform;root.SetParent(owner,false);
        var pipeline=Resources.Load<UniversalRenderPipelineAsset>("Rendering/FarmPipeline");
        if(pipeline!=null){GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;}
        foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None))camera.enabled=false;
        Camera=new GameObject("Caméra 2.5D").AddComponent<Camera>();Camera.transform.SetParent(root,false);
        Camera.orthographic=false;Camera.fieldOfView=39;Camera.nearClipPlane=.1f;Camera.farClipPlane=140;
        Camera.clearFlags=CameraClearFlags.SolidColor;Camera.backgroundColor=new Color(.26f,.34f,.30f);
        Camera.depth=100;Camera.allowHDR=true;Camera.GetUniversalAdditionalCameraData().renderShadows=true;
        focus=new GameObject("Point de suivi").transform;focus.position=new Vector3(7,.3f,4.8f);focus.SetParent(root,true);
        BuildGround();BuildVillage();BuildLighting();BuildPlayer();
        targetRenderer=GroundCard("Case visée",art.Marker,new Vector3(0,.065f,0),new Vector2(.97f,.95f));
        targetRenderer.gameObject.SetActive(false);
        state.PlotChanged+=Refresh;
        for(int z=0;z<FarmSimulation.Height;z++)for(int x=0;x<FarmSimulation.Width;x++)Refresh(x,z);
        UpdateCamera(1,true);UpdateLighting();UpdateCards();
    }

    Material Paper(Texture texture)
    {
        if(paperMaterials.TryGetValue(texture,out var m))return m;
        var shader=Shader.Find("Farm/PaperCutout");if(shader==null)throw new InvalidOperationException("Shader Farm/PaperCutout absent.");
        m=new Material(shader){name="Papier éclairé — "+texture.name};m.SetTexture("_BaseMap",texture);m.SetColor("_BaseColor",Color.white);
        materials.Add(m);paperMaterials[texture]=m;return m;
    }
    Mesh SpriteMesh(Sprite sprite)
    {
        if(spriteMeshes.TryGetValue(sprite,out var mesh))return mesh;
        float ratio=sprite.rect.width/sprite.rect.height;float u=sprite.rect.x/sprite.texture.width,v=sprite.rect.y/sprite.texture.height;
        float u1=sprite.rect.xMax/sprite.texture.width,v1=sprite.rect.yMax/sprite.texture.height;
        mesh=new Mesh{name="Carte "+sprite.name};
        mesh.vertices=new[]{new Vector3(-ratio/2,0,0),new Vector3(ratio/2,0,0),new Vector3(ratio/2,1,0),new Vector3(-ratio/2,1,0)};
        mesh.uv=new[]{new Vector2(u,v),new Vector2(u1,v),new Vector2(u1,v1),new Vector2(u,v1)};
        mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateNormals();mesh.RecalculateBounds();
        meshes.Add(mesh);spriteMeshes[sprite]=mesh;return mesh;
    }
    MeshRenderer Card(string name,Sprite sprite,Vector3 position,float height,Transform parent=null,bool faceCamera=true)
    {
        var g=new GameObject(name);g.transform.SetParent(parent??root,false);g.transform.localPosition=position;g.transform.localScale=Vector3.one*height;
        g.AddComponent<MeshFilter>().sharedMesh=SpriteMesh(sprite);
        var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=Paper(sprite.texture);
        r.shadowCastingMode=ShadowCastingMode.TwoSided;r.receiveShadows=true;
        if(faceCamera)
        {
            cards.Add(g.transform);
            // Préserver le ratio de l’illustration vue par la caméra inclinée.
            g.transform.localScale=new Vector3(height,height/Mathf.Cos(CameraPitch*Mathf.Deg2Rad),height);
        }
        return r;
    }
    MeshRenderer GroundCard(string name,Sprite sprite,Vector3 position,Vector2 size,Transform parent=null)
    {
        var r=Card(name,sprite,position,1,parent,false);
        r.transform.localRotation=Quaternion.Euler(90,0,0);
        r.transform.localScale=new Vector3(size.x/(sprite.rect.width/sprite.rect.height),size.y,1);
        r.transform.localPosition=position-new Vector3(0,0,size.y/2);
        r.shadowCastingMode=ShadowCastingMode.Off;return r;
    }
    MeshRenderer ContactShadow(string name,Vector3 position,Vector2 size)
    {
        if(contactMaterial==null)
        {
            contactMaterial=new Material(Shader.Find("Farm/ContactShadow")){name="Ombres douces au pied"};
            contactMaterial.SetTexture("_BaseMap",art.Shadow.texture);materials.Add(contactMaterial);
        }
        var r=GroundCard(name,art.Shadow,position,size);r.sharedMaterial=contactMaterial;r.receiveShadows=false;
        return r;
    }
    MeshRenderer Prop(FarmArt.Kind kind,Vector2 p,float height,bool solid=false)
    {
        var r=Card(kind.ToString(),art.GetSprite(kind),new Vector3(p.x,GroundHeight(p.x,p.y),p.y),height);
        float width=height*art.GetSprite(kind).rect.width/art.GetSprite(kind).rect.height;
        ContactShadow("Ancrage "+kind,r.transform.position+Vector3.up*.014f,new Vector2(width*.78f,Mathf.Max(.22f,height*.20f)));
        if(solid)
        {
            var g=new GameObject("Volume "+kind);g.transform.SetParent(root,false);g.transform.position=r.transform.position+new Vector3(0,.65f,0);
            var box=g.AddComponent<BoxCollider>();box.size=kind==FarmArt.Kind.Oak?new Vector3(.55f,1.3f,.55f):new Vector3(1.9f,1.3f,.85f);
        }
        return r;
    }
    void BuildGround()
    {
        const int cols=81,rows=81;var vertices=new Vector3[cols*rows];var uv=new Vector2[vertices.Length];
        var indices=new int[(cols-1)*(rows-1)*6];int t=0;
        for(int z=0;z<rows;z++)for(int x=0;x<cols;x++)
        {
            float px=-35+85f*x/(cols-1),pz=-30+85f*z/(rows-1);int i=z*cols+x;
            vertices[i]=new Vector3(px,GroundHeight(px,pz),pz);uv[i]=new Vector2((px+6)/26f,(pz+6)/23f);
            if(x<cols-1&&z<rows-1){indices[t++]=i;indices[t++]=i+cols;indices[t++]=i+1;indices[t++]=i+1;indices[t++]=i+cols;indices[t++]=i+cols+1;}
        }
        var mesh=new Mesh{name="Sol 3D avec relief"};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=indices;mesh.RecalculateNormals();meshes.Add(mesh);
        var ground=new GameObject("Sol");ground.transform.SetParent(root,false);ground.AddComponent<MeshFilter>().sharedMesh=mesh;
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Sol peint éclairé"};
        var terrainTexture=Resources.Load<Texture2D>("Art/clearing-ground");
        terrainTexture.wrapMode=TextureWrapMode.Mirror;
        m.SetTexture("_BaseMap",terrainTexture);m.SetFloat("_Smoothness",0);materials.Add(m);
        ground.AddComponent<MeshRenderer>().sharedMaterial=m;ground.AddComponent<MeshCollider>().sharedMesh=mesh;
        GroundCard("Mare",art.Pond,new Vector3(11.8f,.015f,3.2f),new Vector2(2.6f,1.8f));
        var pondVolume=new GameObject("Bord de mare");pondVolume.transform.SetParent(root,false);pondVolume.transform.position=new Vector3(11.8f,.4f,3.2f);
        pondVolume.AddComponent<BoxCollider>().size=new Vector3(1.9f,.8f,1.1f);
    }
    void BuildVillage()
    {
        house=Prop(FarmArt.Kind.Cottage,new Vector2(2,7.1f),3.1f,true);
        Prop(FarmArt.Kind.Shop,new Vector2(11.8f,7.2f),2.7f,true);
        Prop(FarmArt.Kind.Crate,new Vector2(10.1f,7.1f),.85f);
        Prop(FarmArt.Kind.Crate,new Vector2(1.2f,6.6f),.65f);
        Prop(FarmArt.Kind.Reeds,new Vector2(12.8f,3.6f),1.25f);
        Prop(FarmArt.Kind.Reeds,new Vector2(10.5f,2.7f),.9f);
        var rng=new System.Random(28);
        for(int i=0;i<10;i++)Prop(FarmArt.Kind.Oak,new Vector2(-3+i*2.25f,9.6f+(float)rng.NextDouble()*.6f),3.4f+(float)rng.NextDouble()*.7f,true);
        foreach(var p in new[]{new Vector2(-2,3),new Vector2(-2,6),new Vector2(16.5f,5),new Vector2(17,8),new Vector2(-1,-1),new Vector2(16,-1)})
            Prop(FarmArt.Kind.Oak,p,3.8f,true);
        for(int i=0;i<26;i++)
        {
            float x=i%2==0?-.9f+(float)rng.NextDouble()*1.5f:14.2f+(float)rng.NextDouble()*1.5f;
            Prop(i%3==0?FarmArt.Kind.Flowers:FarmArt.Kind.Reeds,new Vector2(x,-.3f+(i/2)*.73f),.6f+(float)rng.NextDouble()*.4f);
        }
        foreach(var p in new[]{new Vector2(2.8f,6.7f),new Vector2(13.3f,6.8f),new Vector2(2.6f,1),new Vector2(9.7f,1.4f),new Vector2(10.4f,5.7f)})
            Prop(FarmArt.Kind.Flowers,p,.65f);
    }
    void BuildLighting()
    {
        foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))light.enabled=false;
        sun=new GameObject("Soleil").AddComponent<Light>();sun.transform.SetParent(root,false);sun.type=LightType.Directional;
        sun.shadows=LightShadows.Soft;sun.shadowStrength=.8f;sun.shadowBias=.015f;sun.shadowNormalBias=.15f;
        moon=new GameObject("Lune").AddComponent<Light>();moon.transform.SetParent(root,false);moon.type=LightType.Directional;
        moon.color=new Color(.46f,.57f,.91f);moon.shadows=LightShadows.None;moon.transform.rotation=Quaternion.Euler(55,-35,0);
        foreach(var p in new[]{new Vector3(2.9f,1.25f,6.65f),new Vector3(12.8f,1.4f,6.7f),new Vector3(6.5f,.8f,7.7f)})
        {
            var light=new GameObject("Lanterne").AddComponent<Light>();light.transform.SetParent(root,false);light.transform.position=p;
            light.type=LightType.Point;light.range=4.4f;light.color=new Color(1,.60f,.26f);light.shadows=LightShadows.None;lanterns.Add(light);
        }
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;
        RenderSettings.fogStartDistance=25;RenderSettings.fogEndDistance=50;
    }
    void BuildPlayer()
    {
        Player=new GameObject("Fermier — corps 3D et squelette papier").transform;Player.SetParent(root,false);Player.position=Position(6,2);
        controller=Player.gameObject.AddComponent<CharacterController>();controller.height=1.15f;controller.radius=.24f;
        controller.center=new Vector3(0,.58f,0);controller.minMoveDistance=0;controller.stepOffset=.20f;controller.skinWidth=.035f;
        actor=new GameObject("Rig du fermier").transform;actor.SetParent(Player,false);
        float h=1.45f,ratio=art.GetSprite(FarmArt.Kind.Farmer).rect.width/art.GetSprite(FarmArt.Kind.Farmer).rect.height;
        head=new GameObject("Tête").transform;head.SetParent(actor,false);head.localPosition=new Vector3(0,h*.58f,0);
        torso=new GameObject("Buste").transform;torso.SetParent(actor,false);torso.localPosition=new Vector3(0,h*.23f,0);
        var a=Card("Tête illustrée",art.FarmerPart(0),Vector3.zero,h*.42f,head,false);actorRenderers.Add(a);
        a=Card("Buste illustré",art.FarmerPart(1),Vector3.zero,h*.35f,torso,false);actorRenderers.Add(a);
        leftLeg=new GameObject("Jambe gauche").transform;leftLeg.SetParent(actor,false);leftLeg.localPosition=new Vector3(-ratio*h*.25f,h*.23f,0);
        rightLeg=new GameObject("Jambe droite").transform;rightLeg.SetParent(actor,false);rightLeg.localPosition=new Vector3(ratio*h*.25f,h*.23f,0);
        a=Card("Pied gauche",art.FarmerPart(2),new Vector3(0,-h*.23f,0),h*.23f,leftLeg,false);actorRenderers.Add(a);
        a=Card("Pied droit",art.FarmerPart(3),new Vector3(0,-h*.23f,0),h*.23f,rightLeg,false);actorRenderers.Add(a);
        actor.localScale=new Vector3(1,1/Mathf.Cos(CameraPitch*Mathf.Deg2Rad),1);
        cards.Add(actor);
        playerShadow=ContactShadow("Ancrage du fermier",Position(6,2),new Vector2(.8f,.44f));
    }
    public void Update(float delta,Vector2 input,bool canMove,Vector2 cameraStick,bool orbit,FarmTool tool)
    {
        if(orbit&&canMove)yaw=Mathf.Clamp(yaw+cameraStick.x*70*delta,-45,45);
        var right=Camera.transform.right;right.y=0;right.Normalize();
        var forward=Camera.transform.forward;forward.y=0;forward.Normalize();
        Vector3 movement=(right*input.x+forward*input.y);
        if(!canMove)movement=Vector3.zero;
        LastMoveMagnitude=movement.magnitude;
        if(movement.sqrMagnitude>.01f)Facing=new Vector2(movement.x,movement.z).normalized;
        if(Application.isPlaying)controller.Move((movement*3.2f+Vector3.down*2)*delta);
        else Player.position+=movement*3.2f*delta;
        var pos=Player.position;pos.x=Mathf.Clamp(pos.x,.3f,14.7f);pos.z=Mathf.Clamp(pos.z,.3f,10.7f);
        if(!Application.isPlaying)pos.y=GroundHeight(pos.x,pos.z)+.025f;Player.position=pos;
        float stride=canMove?Mathf.Sin(Time.time*11)*LastMoveMagnitude:0;
        leftLeg.localRotation=Quaternion.Euler(0,0,stride*10);rightLeg.localRotation=Quaternion.Euler(0,0,-stride*10);
        head.localRotation=Quaternion.Euler(0,0,stride*1.4f);
        torso.localRotation=Quaternion.Euler(0,0,stride*2f+(Time.time<actionUntil?-7f:0));
        actor.localPosition=new Vector3(0,Mathf.Abs(stride)*.035f,0);
        if(canMove&&input.x>.15f)actor.localScale=new Vector3(1,actor.localScale.y,1);
        else if(canMove&&input.x<-.15f)actor.localScale=new Vector3(-1,actor.localScale.y,1);
        playerShadow.transform.position=new Vector3(pos.x,GroundHeight(pos.x,pos.z)+.018f,pos.z-.22f);
        UpdateCamera(delta,false);UpdateCards();UpdateLighting();
        var t=Target();targetRenderer.gameObject.SetActive(canMove&&(int)tool<4&&FarmSimulation.Cultivable(t.x,t.y));
        targetRenderer.transform.position=Position(t.x,t.y)+new Vector3(0,.045f,-.475f);
        // Léger mouvement des feuilles : reste un plan vertical dans le monde 3D.
    }
    public void TriggerAction()=>actionUntil=Time.time+.22f;
    void UpdateCamera(float delta,bool immediate)
    {
        // Suivi réel du personnage, limité au centre de la clairière.
        Vector3 desired=new Vector3(Mathf.Clamp(Player.position.x,3.8f,10.8f),.3f,Mathf.Clamp(Player.position.z+1.6f,3.2f,7.8f));
        if(immediate)focus.position=desired;else focus.position=Vector3.Lerp(focus.position,desired,1-Mathf.Exp(-delta*3));
        Camera.transform.rotation=Quaternion.Euler(CameraPitch,yaw,0);Camera.transform.position=focus.position-Camera.transform.forward*CameraDistance;
    }
    void UpdateCards()
    {
        for(int i=cards.Count-1;i>=0;i--){if(cards[i]==null){cards.RemoveAt(i);continue;}cards[i].rotation=Quaternion.Euler(0,yaw,0);}
    }
    public void UpdateLighting()
    {
        float h=(float)state.Hour;float day=Mathf.SmoothStep(0,1,Mathf.Clamp01(Mathf.Sin((h-6)/12*Mathf.PI)*2));
        sun.transform.rotation=Quaternion.Euler(Mathf.Lerp(20,65,day),Mathf.Lerp(-60,60,h/24),0);
        sun.color=Color.Lerp(new Color(.75f,.48f,.35f),new Color(.96f,.95f,.90f),day);
        sun.intensity=Mathf.Lerp(0,1.15f,day);moon.intensity=Mathf.Lerp(.24f,0,day);
        RenderSettings.ambientLight=Color.Lerp(new Color(.17f,.20f,.31f),new Color(.40f,.44f,.36f),day);
        RenderSettings.fogColor=Color.Lerp(new Color(.11f,.15f,.23f),new Color(.43f,.49f,.36f),day);
        Camera.backgroundColor=RenderSettings.fogColor;
        foreach(var light in lanterns)light.intensity=Mathf.Lerp(2.4f,.1f,day);
        if(contactMaterial!=null)contactMaterial.SetFloat("_Strength",Mathf.Lerp(.45f,.95f,day));
    }
    public Vector2Int Target()
    {
        var p=new Vector2(Player.position.x,Player.position.z)+Facing*.85f;
        return new Vector2Int(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.y));
    }
    public void Teleport(Vector3 p,Vector2 facing)
    {
        controller.enabled=false;Player.position=p;controller.enabled=true;Facing=facing;UpdateCamera(1,true);
    }
    public void SetHouseTint(Color color)=>SetTint(house,color);
    public void SetOutfitTint(Color color){foreach(var r in actorRenderers)SetTint(r,color);}
    void SetTint(MeshRenderer r,Color color){r.GetPropertyBlock(tint);tint.SetColor("_BaseColor",color);r.SetPropertyBlock(tint);}
    public void Refresh(int x,int z)
    {
        var key=new Vector2Int(x,z);if(plots.TryGetValue(key,out var previous))FarmArt.Release(previous);
        var p=state.Plots[x,z];if(!p.Tilled){plots.Remove(key);return;}
        var g=new GameObject("Parcelle "+x+","+z);g.transform.SetParent(root,false);g.transform.position=Position(x,z);plots[key]=g;
        GroundCard("Terre",p.WateredDay==state.Day?art.WetSoil:art.DrySoil,new Vector3(0,.005f,0),new Vector2(.95f,.93f),g.transform);
        if(p.Crop<0)return;
        if(p.Stage==PlantStage.Seed)
        {
            for(int i=0;i<3;i++)GroundCard("Graine",art.Seed,new Vector3((i-1)*.2f,.015f,0),new Vector2(.075f,.055f),g.transform);
        }
        else
        {
            bool mature=p.Stage==PlantStage.Mature;float height=mature?(p.Crop==2?.75f:.7f):.36f;
            ContactShadow("Ancrage culture",g.transform.position+Vector3.up*.009f,new Vector2(.62f,.34f)).transform.SetParent(g.transform,true);
            Card("Culture "+FarmSimulation.Crops[p.Crop].Name,mature?art.CropSprite(p.Crop):art.GetSprite(FarmArt.Kind.Sprout),Vector3.zero,height,g.transform);
        }
    }
    public void Dispose()
    {
        state.PlotChanged-=Refresh;
        if(root!=null)FarmArt.Release(root.gameObject);
        foreach(var m in materials)FarmArt.Release(m);foreach(var mesh in meshes)FarmArt.Release(mesh);
    }
}
