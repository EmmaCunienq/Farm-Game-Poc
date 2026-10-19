using System;
using System.Collections.Generic;
using UnityEngine;

// Textures et sprites consommés par les cartes 3D et l’interface.
public sealed partial class FarmArt : IDisposable
{
    public enum Kind { Cottage, Shop, Oak, Farmer, Carrots, Sprout, Reeds, Flowers, Crate, Radishes, Pumpkin }
    readonly Texture2D atlas;
    readonly Texture2D extraCrops;
    readonly Dictionary<Kind, Sprite> cutouts = new();
    readonly List<Texture2D> textures = new();
    readonly List<Sprite> sprites = new();
    public Sprite DrySoil { get; }
    public Sprite WetSoil { get; }
    public Sprite Shadow { get; }
    public Sprite Marker { get; }
    public Sprite Seed { get; }
    public Sprite Pond { get; }

    public FarmArt()
    {
        atlas = Resources.Load<Texture2D>("Art/village-atlas");
        extraCrops = Resources.Load<Texture2D>("Art/extra-crops");
        DrySoil = MakeSprite(Soil(false)); WetSoil = MakeSprite(Soil(true));
        Shadow = MakeSprite(Ellipse(64, 32, new Color(.10f, .13f, .10f, .25f), false));
        Marker = MakeSprite(Ellipse(96, 64, new Color(.98f, .81f, .44f, .85f), true));
        Seed = MakeSprite(Ellipse(24, 24, new Color(.83f, .63f, .33f), false));
        Pond = MakeSprite(Water());
        if (atlas == null) Debug.LogError("Atlas absent : Assets/Resources/Art/village-atlas.png");
    }

    public Sprite GetSprite(Kind kind)
    {
        if (cutouts.TryGetValue(kind, out var cached)) return cached;
        if(kind==Kind.Radishes||kind==Kind.Pumpkin)
        {
            if(extraCrops==null)return Seed;
            int cropLeft=kind==Kind.Radishes?0:extraCrops.width/2;
            var cropRect=Trim(extraCrops,new Rect(cropLeft,0,extraCrops.width/2,extraCrops.height));
            var extra=Sprite.Create(extraCrops,cropRect,new Vector2(.5f,0),100f,0,SpriteMeshType.FullRect);
            sprites.Add(extra);cutouts[kind]=extra;return extra;
        }
        if (atlas == null) return Seed;
        // Bornes propres à chaque dessin : la génération n'a pas laissé des cellules égales.
        // Coordonnées en pixels depuis le haut gauche de l'atlas source 1254 x 1254.
        Rect[] regions = {
            new Rect(4,31,429,421), new Rect(434,62,400,406), new Rect(830,14,420,455),
            new Rect(42,474,315,403), new Rect(430,516,412,336), new Rect(945,584,233,209),
            new Rect(22,886,395,356), new Rect(438,929,396,284), new Rect(847,890,397,337)
        };
        var source = regions[(int)kind];
        int left = Mathf.RoundToInt(source.x / 1254f * atlas.width), right = Mathf.RoundToInt(source.xMax / 1254f * atlas.width);
        int bottom = atlas.height - Mathf.RoundToInt(source.yMax / 1254f * atlas.height);
        int top = atlas.height - Mathf.RoundToInt(source.y / 1254f * atlas.height);
        Rect rect = new Rect(left, bottom, right-left, top-bottom);
        if (atlas.isReadable)
        {
            var pixels = atlas.GetPixels32(); int minX=right, minY=top, maxX=left, maxY=bottom;
            for (int y=bottom; y<top; y++) for (int x=left; x<right; x++)
                if (pixels[y*atlas.width+x].a > 24) { minX=Mathf.Min(minX,x); maxX=Mathf.Max(maxX,x); minY=Mathf.Min(minY,y); maxY=Mathf.Max(maxY,y); }
            if (minX <= maxX) rect = new Rect(minX,minY,maxX-minX+1,maxY-minY+1);
        }
        var sprite = Sprite.Create(atlas, rect, new Vector2(.5f,0), 100f, 0, SpriteMeshType.FullRect);
        sprite.name = kind.ToString(); sprites.Add(sprite); cutouts[kind]=sprite; return sprite;
    }

    static Rect Trim(Texture2D texture,Rect region)
    {
        if(!texture.isReadable)return region;
        var pixels=texture.GetPixels32();int left=(int)region.x,right=(int)region.xMax,bottom=(int)region.y,top=(int)region.yMax;
        int minX=right,minY=top,maxX=left,maxY=bottom;
        for(int y=bottom;y<top;y++)for(int x=left;x<right;x++)if(pixels[y*texture.width+x].a>24){minX=Mathf.Min(x,minX);minY=Mathf.Min(y,minY);maxX=Mathf.Max(x,maxX);maxY=Mathf.Max(y,maxY);}
        return minX<=maxX?new Rect(minX,minY,maxX-minX+1,maxY-minY+1):region;
    }
    public Sprite CropSprite(int kind)=>GetSprite(kind==0?Kind.Carrots:kind==1?Kind.Radishes:Kind.Pumpkin);
    public Sprite FarmerPart(int part)
    {
        var all=GetSprite(Kind.Farmer);var r=all.rect;
        Rect piece=part==0?new Rect(r.x,r.y+r.height*.58f,r.width,r.height*.42f):
            part==1?new Rect(r.x,r.y+r.height*.23f,r.width,r.height*.35f):
            new Rect(r.x+(part==3?r.width*.5f:0),r.y,r.width*.5f,r.height*.23f);
        var s=Sprite.Create(all.texture,piece,new Vector2(.5f,0),100f,0,SpriteMeshType.FullRect);sprites.Add(s);return s;
    }

    Texture2D Texture(int width,int height) { var t=new Texture2D(width,height,TextureFormat.RGBA32,false); t.filterMode=FilterMode.Bilinear; t.wrapMode=TextureWrapMode.Clamp; textures.Add(t); return t; }
    Sprite MakeSprite(Texture2D t) { var s=Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(.5f,.5f),t.width); sprites.Add(s); return s; }
    static float Grain(int x,int y) { uint n=unchecked((uint)(x*374761393+y*668265263)); n=unchecked((n^(n>>13))*1274126177); return ((n^(n>>16))&255)/255f; }

    Texture2D Soil(bool wet)
    {
        var t=Texture(128,88); var pixels=new Color[128*88];
        for(int y=0;y<88;y++) for(int x=0;x<128;x++)
        {
            float u=(x-63.5f)/61f,v=(y-43.5f)/40f;
            float edge=Mathf.Pow(Mathf.Abs(u),5)+Mathf.Pow(Mathf.Abs(v),5);
            float rim=1f+.045f*Mathf.Sin(x*.27f+y*.19f); Color c=Color.clear;
            if(edge<rim) {
                if(edge>rim-.14f) c=new Color(.20f,.20f,.13f,.88f);
                else {
                    float noise=(Grain(x,y)-.5f)*.07f;
                    float line=Mathf.Sin(y*.36f+Mathf.Sin(x*.06f)*.9f)>.72f ? -.055f : 0f;
                    c=(wet?new Color(.28f,.32f,.25f):new Color(.43f,.35f,.24f))+new Color(noise+line,noise+line,noise+line,0);
                }
            }
            pixels[y*128+x]=c;
        }
        t.SetPixels(pixels);t.Apply();return t;
    }

    Texture2D Ellipse(int w,int h,Color color,bool ring)
    {
        var t=Texture(w,h);var pixels=new Color[w*h];
        for(int y=0;y<h;y++) for(int x=0;x<w;x++) {
            float u=(x-(w-1)*.5f)/(w*.48f),v=(y-(h-1)*.5f)/(h*.48f),d=u*u+v*v;
            var c=color;c.a*=ring?(d>.74f?Mathf.Clamp01((1-d)*12):0):Mathf.Clamp01((1-d)*3);pixels[y*w+x]=c;
        }
        t.SetPixels(pixels);t.Apply();return t;
    }

    Texture2D Water()
    {
        var t=Texture(192,128);var pixels=new Color[192*128];
        for(int y=0;y<128;y++) for(int x=0;x<192;x++) {
            float u=(x-95.5f)/91f,v=(y-63.5f)/59f,d=u*u+v*v;
            float rim=1+.055f*Mathf.Sin(x*.09f+y*.05f);Color c=Color.clear;
            if(d<rim) { c=d>rim-.11f?new Color(.29f,.37f,.26f):new Color(.36f,.55f,.54f);
                if(d<.70f && Mathf.Sin(y*.32f+Mathf.Sin(x*.08f))>.96f) c=new Color(.66f,.76f,.66f);
                float n=(Grain(x,y)-.5f)*.035f;c+=new Color(n,n,n,0); }
            pixels[y*192+x]=c;
        }
        t.SetPixels(pixels);t.Apply();return t;
    }

    public Texture2D Panel(Color fill,Color border)
    {
        const int w=96,h=48;var t=Texture(w,h);var pixels=new Color[w*h];
        for(int y=0;y<h;y++)for(int x=0;x<w;x++) {
            float dx=Mathf.Max(7-x,x-(w-8)),dy=Mathf.Max(7-y,y-(h-8));
            bool inside=Mathf.Max(dx,0)*Mathf.Max(dx,0)+Mathf.Max(dy,0)*Mathf.Max(dy,0)<=49;
            Color c=inside?(x<2||x>=w-2||y<2||y>=h-2?border:fill):Color.clear;
            if(inside) {float n=(Grain(x,y)-.5f)*.015f;c+=new Color(n,n,n,0);}pixels[y*w+x]=c;
        }
        t.SetPixels(pixels);t.Apply();return t;
    }

    // Un rendu de contrôle dans l'éditeur n'a pas de fin de frame pour Destroy.
    public static void Release(UnityEngine.Object item)
    {
        if (Application.isPlaying) UnityEngine.Object.Destroy(item);
        else UnityEngine.Object.DestroyImmediate(item);
    }
    public void Dispose() { foreach(var s in sprites) Release(s); foreach(var t in textures) Release(t); }
}
