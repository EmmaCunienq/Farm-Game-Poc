using System.Collections.Generic;
using UnityEngine;

public sealed partial class FarmArt
{
    readonly Dictionary<string,Sprite> avatarParts=new();
    Texture2D characterAtlas;
    static readonly Color[] skinColors={new(.98f,.82f,.65f),new(.90f,.64f,.43f),new(.70f,.43f,.25f),new(.43f,.25f,.15f)};
    static readonly Color[] hairColors={new(.43f,.25f,.13f),new(.80f,.61f,.29f),new(.23f,.17f,.13f),new(.66f,.31f,.15f)};
    static readonly float[] faceCenters={228,634,1044};
    static Color Repaint(Color c,Color source,Color target)=>new(Mathf.Clamp01(c.r*target.r/source.r),Mathf.Clamp01(c.g*target.g/source.g),Mathf.Clamp01(c.b*target.b/source.b),c.a);
    static bool IsSkin(Color c)=>c.r>.47f&&c.g/c.r>.63f&&c.g/c.r<.91f&&c.b/c.r>.28f&&c.b/c.r<.76f;
    public Sprite AvatarPart(FarmAppearance a,int part)
    {
        string key=part==0?$"head:{a.Skin}:{a.Hair}:{a.HairColor}:{a.Eyes}:{a.Mouth}":part==1?$"top:{a.Top}:{a.Skin}":$"leg:{a.Bottom}:{a.Shoes}:{part}";
        if(avatarParts.TryGetValue(key,out var cached))return cached;
        characterAtlas??=ArtResource("farmer-paper-parts");var source=Pixels(characterAtlas);
        int w=part==0?384:part==1?384:192,h=part==0?350:part==1?330:420;
        var t=Texture(w,h);t.name=key;var pixels=new Color[w*h];
        float scaleX=characterAtlas.width/1280f,scaleY=characterAtlas.height/1280f;
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)
        {
            float px,py;
            if(part==0)
            {
                const float imageScale=96f/112;
                px=faceCenters[a.Hair]+(x-w*.5f)/imageScale;py=295-(y-85)/imageScale;
                if(px<a.Hair*1280f/3||px>=(a.Hair+1)*1280f/3||py<0||py>=410)continue;
            }
            else if(part==1){px=a.Top*1280f/3+x/(float)w*(1280f/3);py=795-y/(float)h*385;}
            else{px=a.Bottom*1280f/3+(part==2?0:1280f/6)+x/(float)w*(1280f/6);py=1254-y/(float)h*459;}
            int sx=Mathf.Clamp(Mathf.RoundToInt(px*scaleX),0,characterAtlas.width-1),sy=Mathf.Clamp(characterAtlas.height-1-Mathf.RoundToInt(py*scaleY),0,characterAtlas.height-1);
            Color c=source[sy*characterAtlas.width+sx];if(c.a<.02f)continue;
            if(part==0)
            {
                float face=Mathf.Pow((x-192)/150f,2)+Mathf.Pow((y-94)/115f,2);
                if(face<1&&IsSkin(c))c=Repaint(c,new Color(.95f,.72f,.45f),skinColors[a.Skin]);
                else if((y>116||Mathf.Abs(x-192)>125)&&c.r>.19f&&c.g>.08f&&c.g/c.r<.73f)c=Repaint(c,hairColors[0],hairColors[a.HairColor]);
                if(a.Eyes!=0&&y>64&&y<110&&(Mathf.Pow((x-145)/15f,2)+Mathf.Pow((y-85)/24f,2)<1||Mathf.Pow((x-240)/15f,2)+Mathf.Pow((y-85)/24f,2)<1)&&c.r>.12f&&c.r<.65f&&c.g/c.r<.85f)
                    c=Repaint(c,new Color(.36f,.22f,.11f),a.Eyes==1?new Color(.24f,.45f,.57f):new Color(.29f,.43f,.25f));
            }
            else if(part==1)
            {
                bool hands=y<105&&(x<85||x>299),neck=y>280&&x>147&&x<237;
                if((hands||neck)&&IsSkin(c))c=Repaint(c,new Color(.95f,.72f,.45f),skinColors[a.Skin]);
            }
            else if(a.Shoes!=0&&y<145&&c.r>.18f&&c.g/c.r<.85f)
                c=Repaint(c,new Color(.47f,.28f,.13f),a.Shoes==1?new Color(.63f,.27f,.18f):new Color(.87f,.79f,.62f));
            pixels[y*w+x]=c;
        }
        if(part==0&&a.Mouth!=0)PaintMouth(pixels,w,h,a);
        if(part==1&&a.Top==2)
            for(int y=278;y<320;y++)for(int x=168;x<216;x++)
                if(Mathf.Pow((x-192)/23f,2)+Mathf.Pow((y-296)/25f,2)<1&&pixels[y*w+x].a<.05f)
                    pixels[y*w+x]=skinColors[a.Skin]*(.97f+Grain(x,y)*.06f);
        t.SetPixels(pixels);t.Apply();var sprite=MakeSprite(t);sprite.name=key;avatarParts[key]=sprite;return sprite;
    }
    static void PaintMouth(Color[] pixels,int w,int h,FarmAppearance appearance)
    {
        // Remplacer seulement les traits de la bouche en conservant la peinture du visage.
        for(int y=40;y<66;y++)for(int x=169;x<219;x++)
        {
            var c=pixels[y*w+x];
            if(c.a>.5f&&c.r<.54f&&c.g<.40f)
            {var nearby=pixels[Mathf.Min(h-1,y+20)*w+x];pixels[y*w+x]=nearby.r>.55f?nearby:skinColors[appearance.Skin];}
        }
        Color ink=new(.24f,.13f,.08f);
        for(int y=44;y<62;y++)for(int x=179;x<209;x++)
        {
            float dx=x-194,dy=y-53;
            if(appearance.Mouth==1){if(Mathf.Abs(dy)<1.1f&&Mathf.Abs(dx)<6)pixels[y*w+x]=ink;}
            else if(dx*dx/58+dy*dy/38<1)pixels[y*w+x]=dy>2&&Mathf.Abs(dx)<5?new Color(.99f,.91f,.74f):dy< -2?new Color(.72f,.33f,.27f):ink;
        }
    }
}
