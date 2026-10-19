using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class FarmAppearance
{
    public int Skin, Hair, HairColor, Eyes, Mouth, Top, Bottom, Shoes;
    public FarmAppearance Copy() => (FarmAppearance)MemberwiseClone();
    public int Get(int category) => category switch { 0=>Skin,1=>Hair,2=>HairColor,3=>Eyes,4=>Mouth,5=>Top,6=>Bottom,_=>Shoes };
    public void Set(int category,int value)
    {
        switch(category){case 0:Skin=value;break;case 1:Hair=value;break;case 2:HairColor=value;break;case 3:Eyes=value;break;case 4:Mouth=value;break;case 5:Top=value;break;case 6:Bottom=value;break;case 7:Shoes=value;break;}
    }
    public bool Valid(){for(int i=0;i<8;i++)if(Get(i)<0||Get(i)>=Options[i].Length)return false;return true;}
    public static readonly string[] Categories={"Peau","Coiffure","Couleur des cheveux","Yeux","Bouche","Haut","Bas","Chaussures"};
    public static readonly string[][] Options={
        new[]{"Porcelaine","Pêche","Dorée","Brune"},new[]{"Courts","Carré","Bouclés"},new[]{"Châtain","Blond","Brun","Cuivré"},
        new[]{"Noisette","Bleus","Verts"},new[]{"Sourire","Discrète","Joyeuse"},new[]{"Salopette sauge","Pull terracotta","Marinière"},
        new[]{"Pantalon brun","Jean bleu","Pantalon crème"},new[]{"Bottes brunes","Bottines rouges","Baskets crème"}
    };
}

public sealed class FarmFurnitureDefinition
{
    public readonly string Name;
    public readonly int Width,Depth;
    public FarmFurnitureDefinition(string name,int width,int depth){Name=name;Width=width;Depth=depth;}
}

[Serializable]
public sealed class FarmFurniture
{
    public int Kind,X,Z,Rotation;
    public FarmFurniture Copy() => (FarmFurniture)MemberwiseClone();
    public int Width=>Rotation%2==0?FarmHome.Catalog[Kind].Width:FarmHome.Catalog[Kind].Depth;
    public int Depth=>Rotation%2==0?FarmHome.Catalog[Kind].Depth:FarmHome.Catalog[Kind].Width;
    public bool Contains(int x,int z)=>x>=X&&x<X+Width&&z>=Z&&z<Z+Depth;
}

[Serializable]
public sealed class FarmHome
{
    public const int Width=9,Depth=7;
    public int Wallpaper,Floor;
    public List<FarmFurniture> Furniture=new();
    public static readonly string[] Wallpapers={"Lin fleuri","Rayures sauge","Pêche à pois"};
    public static readonly string[] Floors={"Parquet miel","Dalles crème","Planches foncées"};
    public static readonly FarmFurnitureDefinition[] Catalog={
        new("Lit",2,3),new("Table",2,2),new("Chaise",1,1),new("Canapé",3,1),new("Bibliothèque",2,1),new("Plante",1,1),new("Lampe",1,1),new("Tapis",3,2)
    };
    public static FarmHome Starter()=>new FarmHome {Furniture=new List<FarmFurniture>{
        new(){Kind=0,X=1,Z=3},new(){Kind=1,X=5,Z=3},new(){Kind=2,X=5,Z=2},new(){Kind=5,X=7,Z=5}
    }};
    public static bool Reserved(int x,int z)=>(z<=1&&x>=3&&x<=5)||(z>=5&&(x==0||x==8));
    public int At(int x,int z){for(int i=Furniture.Count-1;i>=0;i--)if(Furniture[i].Contains(x,z))return i;return -1;}
    public bool CanPlace(FarmFurniture item,int ignore,out string message)
    {
        if(item==null||item.Kind<0||item.Kind>=Catalog.Length||item.Rotation<0||item.Rotation>3){message="Meuble incorrect.";return false;}
        if(item.X<0||item.Z<0||item.X+item.Width>Width||item.Z+item.Depth>Depth){message="Le meuble dépasse de la pièce.";return false;}
        for(int z=item.Z;z<item.Z+item.Depth;z++)for(int x=item.X;x<item.X+item.Width;x++)
        {
            if(Reserved(x,z)){message="Laisse l'entrée, le coffre et l'armoire accessibles.";return false;}
            for(int i=0;i<Furniture.Count;i++)
                if(i!=ignore&&Furniture[i].Contains(x,z)&&Furniture[i].Kind!=7&&item.Kind!=7){message="Cette place est déjà occupée.";return false;}
        }
        message="Emplacement libre.";return true;
    }
    public bool Place(FarmFurniture item,int replace,out string message)
    {
        if(!CanPlace(item,replace,out message))return false;
        if(replace>=0&&replace<Furniture.Count)Furniture[replace]=item.Copy();else Furniture.Add(item.Copy());
        message=Catalog[item.Kind].Name+" placé(e).";return true;
    }
    public bool Valid()
    {
        if(Wallpaper<0||Wallpaper>=Wallpapers.Length||Floor<0||Floor>=Floors.Length||Furniture==null||Furniture.Count>100)return false;
        foreach(var item in Furniture)if(item==null||item.Kind<0||item.Kind>=Catalog.Length)return false;
        for(int i=0;i<Furniture.Count;i++)if(!CanPlace(Furniture[i],i,out _))return false;
        return true;
    }
}
