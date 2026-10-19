using System.Collections.Generic;
using UnityEngine;

public sealed partial class FarmWorld25D
{
    public const float HomeOffsetX=80;
    public bool Indoors { get; private set; }
    public bool NearHouse=>!Indoors&&Vector2.Distance(new Vector2(Player.position.x,Player.position.z),new Vector2(2,6.1f))<1.65f;
    public bool NearExit=>Indoors&&Vector2.Distance(new Vector2(Player.position.x,Player.position.z),new Vector2(HomeOffsetX+4.5f,.55f))<1.3f;
    public bool NearWardrobe=>Indoors&&Vector2.Distance(new Vector2(Player.position.x,Player.position.z),new Vector2(HomeOffsetX+.5f,6))<1.75f;
    public bool NearDecorChest=>Indoors&&Vector2.Distance(new Vector2(Player.position.x,Player.position.z),new Vector2(HomeOffsetX+8.5f,6))<1.75f;
    Transform interior,furnitureRoot;
    MeshRenderer roomFloor;
    readonly List<MeshRenderer> roomWalls=new();
    Material[] floorMaterials,wallMaterials;
    public string InteractionHint=>NearHouse?"Clique sur la maison pour entrer":NearExit?"Clique sur le tapis pour sortir":NearWardrobe?"Clique sur l'armoire pour te changer":NearDecorChest?"Clique sur le coffre pour décorer":Indoors?"Coffre : décoration · Armoire : personnage":"";

    static void HomeSurface(MeshRenderer renderer,Material material)
    {
        renderer.sharedMaterial=material;var properties=new MaterialPropertyBlock();
        properties.SetColor("_BaseColor",Color.white);properties.SetTexture("_BaseMap",material.GetTexture("_BaseMap"));
        var scale=material.GetTextureScale("_BaseMap");properties.SetVector("_BaseMap_ST",new Vector4(scale.x,scale.y,0,0));renderer.SetPropertyBlock(properties);
    }
    Material Surface(int style,bool wall)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=(wall?"Mur peint ":"Sol peint ")+style};
        m.SetTexture("_BaseMap",art.HomeSurfaceTexture(style,wall));m.SetFloat("_Smoothness",0);m.SetFloat("_Cull",0);
        m.SetTextureScale("_BaseMap",wall?new Vector2(2,1):new Vector2(1.8f,1.4f));materials.Add(m);return m;
    }
    MeshRenderer RoomPlane(string name,Vector3 center,Vector2 size,Quaternion rotation)
    {
        var g=new GameObject(name);g.transform.SetParent(interior,false);g.transform.localPosition=center;g.transform.localRotation=rotation;
        var mesh=new Mesh{name=name+" — plan illustré"};
        mesh.vertices=new[]{new Vector3(-size.x/2,-size.y/2,0),new Vector3(size.x/2,-size.y/2,0),new Vector3(size.x/2,size.y/2,0),new Vector3(-size.x/2,size.y/2,0)};
        mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateNormals();meshes.Add(mesh);
        g.AddComponent<MeshFilter>().sharedMesh=mesh;
        var box=g.AddComponent<BoxCollider>();box.size=new Vector3(size.x,size.y,.12f);box.center=new Vector3(0,0,.06f);
        return g.AddComponent<MeshRenderer>();
    }
    void Volume(string name,Vector3 center,Vector3 size)
    {
        var g=new GameObject(name);g.transform.SetParent(interior,false);g.transform.localPosition=center;g.AddComponent<BoxCollider>().size=size;
    }
    void Timber(Vector3 center,float width,Quaternion rotation)
    {
        var sprite=art.HomeSprite(11);var r=Card("Poutre illustrée",sprite,center-new Vector3(0,.09f,0),1,interior,false);
        r.transform.localScale=new Vector3(width/(sprite.rect.width/sprite.rect.height),.18f,1);r.transform.localRotation=rotation;
    }
    void BuildInterior()
    {
        interior=new GameObject("Intérieur de la maison").transform;interior.SetParent(root,false);interior.localPosition=new Vector3(HomeOffsetX,0,0);
        floorMaterials=new Material[3];wallMaterials=new Material[3];for(int i=0;i<3;i++){floorMaterials[i]=Surface(i,false);wallMaterials[i]=Surface(i,true);}
        roomFloor=RoomPlane("Sol",new Vector3(4.5f,0,3.5f),new Vector2(9,7),Quaternion.Euler(90,0,0));
        roomWalls.Add(RoomPlane("Mur du fond",new Vector3(4.5f,1.2f,7),new Vector2(9,2.4f),Quaternion.identity));
        roomWalls.Add(RoomPlane("Mur gauche",new Vector3(0,1.2f,3.5f),new Vector2(7,2.4f),Quaternion.Euler(0,-90,0)));
        roomWalls.Add(RoomPlane("Mur droit",new Vector3(9,1.2f,3.5f),new Vector2(7,2.4f),Quaternion.Euler(0,90,0)));
        Timber(new Vector3(4.5f,.14f,6.93f),9,Quaternion.identity);Timber(new Vector3(4.5f,2.38f,6.93f),9,Quaternion.identity);
        Timber(new Vector3(.06f,.14f,3.5f),7,Quaternion.Euler(0,-90,0));Timber(new Vector3(8.94f,.14f,3.5f),7,Quaternion.Euler(0,90,0));
        Card("Fenêtre et rideaux illustrés",art.HomeSprite(10),new Vector3(4.5f,.78f,6.87f),1.25f,interior);
        wardrobeIllustration=Card("Armoire — personnalisation",art.HomeSprite(7),new Vector3(.85f,0,6.2f),1.65f,interior);
        chestIllustration=Card("Coffre — décoration",art.HomeSprite(8),new Vector3(8.3f,0,6.1f),.72f,interior);
        Volume("Volume armoire",new Vector3(.5f,1,6),new Vector3(.85f,2,.8f));Volume("Volume coffre",new Vector3(8.5f,.42f,6),new Vector3(.85f,.84f,.8f));
        ContactShadow("Ancrage armoire",new Vector3(HomeOffsetX+.85f,.012f,6.2f),new Vector2(1.45f,.62f)).transform.SetParent(interior,true);
        ContactShadow("Ancrage coffre",new Vector3(HomeOffsetX+8.3f,.012f,6.1f),new Vector2(1.15f,.55f)).transform.SetParent(interior,true);
        exitIllustration=GroundCard("Seuil — sortie",art.HomeSprite(9),new Vector3(4.5f,.016f,.4f),new Vector2(1.65f,.70f),interior);
        interior.gameObject.SetActive(false);
    }
    public void ApplyHome(FarmHome home)
    {
        HomeSurface(roomFloor,floorMaterials[home.Floor]);foreach(var wall in roomWalls)HomeSurface(wall,wallMaterials[home.Wallpaper]);
        if(furnitureRoot!=null){furnitureRoot.gameObject.SetActive(false);FarmArt.Release(furnitureRoot.gameObject);}
        furnitureRoot=new GameObject("Mobilier placé").transform;furnitureRoot.SetParent(interior,false);
        foreach(var item in home.Furniture)BuildFurniture(item);
    }
    void BuildFurniture(FarmFurniture item)
    {
        var g=new GameObject(FarmHome.Catalog[item.Kind].Name).transform;g.SetParent(furnitureRoot,false);
        g.localPosition=new Vector3(item.X+item.Width*.5f,0,item.Z+item.Depth*.5f);
        if(item.Kind==7)
        {
            GroundCard("Tapis illustré",art.HomeSprite(9),new Vector3(0,.02f,0),new Vector2(2.85f,1.85f),g);
            g.localRotation=Quaternion.Euler(0,item.Rotation*90,0);return;
        }
        float[] heights={1.72f,1.20f,1.18f,1.42f,2.05f,1.15f,1.85f};
        var sprite=art.HomeSprite(item.Kind,item.Rotation%2!=0);
        float height=Mathf.Min(heights[item.Kind],item.Width*.97f/(sprite.rect.width/sprite.rect.height));
        var illustration=Card("Illustration "+FarmHome.Catalog[item.Kind].Name,sprite,Vector3.zero,height,g);
        if(item.Rotation>=2){var s=illustration.transform.localScale;s.x=-s.x;illustration.transform.localScale=s;}
        ContactShadow("Ancrage mobilier",g.position+Vector3.up*.012f,new Vector2(item.Width*.82f,item.Depth*.42f)).transform.SetParent(g,true);
        var box=g.gameObject.AddComponent<BoxCollider>();box.center=new Vector3(0,.55f,0);box.size=new Vector3(item.Width-.18f,1.1f,item.Depth-.18f);
    }
    public void SetIndoors(bool value,bool teleport=true)
    {
        Indoors=value;interior.gameObject.SetActive(value);
        if(teleport)Teleport(value?new Vector3(HomeOffsetX+4.5f,.03f,1.5f):new Vector3(2,.03f,5.7f),value?Vector2.up:Vector2.down);
        UpdateCamera(1,true);UpdateCards();UpdateLighting();
    }
    public bool OccupiedByPlayer(FarmFurniture item)=>Indoors&&item.Kind!=7&&
        Player.position.x>HomeOffsetX+item.X-.24f&&Player.position.x<HomeOffsetX+item.X+item.Width+.24f&&Player.position.z>item.Z-.24f&&Player.position.z<item.Z+item.Depth+.24f;
    public void ApplyAppearance(FarmAppearance appearance)
    {
        for(int i=0;i<actorRenderers.Count;i++)
        {var sprite=art.AvatarPart(appearance,i);actorRenderers[i].GetComponent<MeshFilter>().sharedMesh=SpriteMesh(sprite);actorRenderers[i].sharedMaterial=Paper(sprite.texture);}
    }
}
