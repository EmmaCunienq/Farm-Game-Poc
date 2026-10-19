using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class FarmArt
{
    readonly Dictionary<int,Sprite> homeSprites=new();
    readonly Dictionary<Texture2D,Color32[]> sourcePixels=new();
    Texture2D homeAtlas,homeSideAtlas,surfaceAtlas;
    public const float AvatarHeadHeight=.70f,AvatarTorsoHeight=.46f,AvatarLegHeight=.60f,AvatarTorsoBase=.52f,AvatarHeadBase=.88f,AvatarLegSpacing=.132f;
    Color32[] Pixels(Texture2D source)
    {
        if(!sourcePixels.TryGetValue(source,out var pixels)){pixels=source.GetPixels32();sourcePixels[source]=pixels;}
        return pixels;
    }
    static Texture2D ArtResource(string name)
    {
        var texture=Resources.Load<Texture2D>("Art/"+name);
        if(texture==null||!texture.isReadable)throw new InvalidOperationException("Illustration absente ou non lisible : "+name+". Attends la fin de l'import Unity.");
        return texture;
    }
    Rect SourceRect(Texture2D texture,Rect topLeft)
    {
        float sx=texture.width/1280f,sy=texture.height/1280f;
        int left=Mathf.Clamp(Mathf.RoundToInt(topLeft.x*sx),0,texture.width-1),right=Mathf.Clamp(Mathf.RoundToInt(topLeft.xMax*sx),left+1,texture.width);
        int bottom=Mathf.Clamp(texture.height-Mathf.RoundToInt(topLeft.yMax*sy),0,texture.height-1),top=Mathf.Clamp(texture.height-Mathf.RoundToInt(topLeft.y*sy),bottom+1,texture.height);
        return new Rect(left,bottom,right-left,top-bottom);
    }
    public Sprite HomeSprite(int kind,bool side=false)
    {
        int key=kind+(side&&kind<9?20:0);
        if(homeSprites.TryGetValue(key,out var sprite))return sprite;
        homeAtlas??=ArtResource("home-paper-atlas");
        var regions=new[]{
            new Rect(15,5,421,337),new Rect(451,5,395,327),new Rect(929,0,303,338),
            new Rect(5,344,505,311),new Rect(514,316,299,353),new Rect(840,337,415,328),
            new Rect(138,650,226,335),new Rect(486,672,326,310),new Rect(857,718,385,270),
            new Rect(33,1004,420,236),new Rect(458,984,360,273),new Rect(815,1045,438,126)
        };
        var source=homeAtlas;Rect rect;
        if(side&&kind<9)
        {
            homeSideAtlas??=ArtResource("home-paper-sides");source=homeSideAtlas;
            var sideRegions=new[]{
                new Rect(30,30,500,350),new Rect(531,29,378,360),new Rect(952,28,294,361),
                new Rect(97,386,367,423),new Rect(560,379,272,430),new Rect(855,391,390,422),
                new Rect(148,811,235,434),new Rect(538,797,288,450),new Rect(929,890,320,350)
            };rect=sideRegions[kind];
        }
        else rect=regions[kind];
        var region=MainCutoutBounds(source,SourceRect(source,rect));
        sprite=Sprite.Create(source,region,new Vector2(.5f,0),100,0,SpriteMeshType.FullRect);sprite.name="Mobilier papier "+key;
        sprites.Add(sprite);homeSprites[key]=sprite;return sprite;
    }
    Rect MainCutoutBounds(Texture2D source,Rect region)
    {
        // Certains dessins dépassent les cellules : cadrer le sujet principal,
        // sans embarquer un fragment isolé de l'illustration voisine.
        var pixels=Pixels(source);int width=(int)region.width,height=(int)region.height,left=(int)region.x,bottom=(int)region.y;
        var visited=new bool[width*height];var queue=new Queue<int>();int largest=0;Rect result=region;
        for(int start=0;start<visited.Length;start++)
        {
            if(visited[start])continue;visited[start]=true;
            if(pixels[(bottom+start/width)*source.width+left+start%width].a<=24)continue;
            int count=0,minX=width,minY=height,maxX=0,maxY=0;queue.Enqueue(start);
            while(queue.Count>0)
            {
                int current=queue.Dequeue(),x=current%width,y=current/width;count++;
                minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                {
                    int nx=x+dx,ny=y+dy;if(nx<0||ny<0||nx>=width||ny>=height)continue;
                    int index=ny*width+nx;if(visited[index])continue;visited[index]=true;
                    if(pixels[(bottom+ny)*source.width+left+nx].a>24)queue.Enqueue(index);
                }
            }
            if(count>largest)
            {
                largest=count;minX=Mathf.Max(0,minX-2);minY=Mathf.Max(0,minY-2);maxX=Mathf.Min(width-1,maxX+2);maxY=Mathf.Min(height-1,maxY+2);
                result=new Rect(left+minX,bottom+minY,maxX-minX+1,maxY-minY+1);
            }
        }
        return result;
    }
    public Texture2D HomeSurfaceTexture(int style,bool wall)
    {
        surfaceAtlas??=ArtResource("home-painted-surfaces");
        int left=Mathf.RoundToInt(style*surfaceAtlas.width/3f),right=Mathf.RoundToInt((style+1)*surfaceAtlas.width/3f);
        int bottom=wall?surfaceAtlas.height/2:0,height=surfaceAtlas.height/2;
        var t=Texture(right-left-4,height-4);t.name=(wall?"Papier peint illustré ":"Sol illustré ")+style;
        var data=Pixels(surfaceAtlas);var pixels=new Color32[t.width*t.height];
        for(int y=0;y<t.height;y++)Array.Copy(data,(bottom+y+2)*surfaceAtlas.width+left+2,pixels,y*t.width,t.width);
        t.SetPixels32(pixels);t.wrapMode=TextureWrapMode.Mirror;t.Apply();return t;
    }
}
