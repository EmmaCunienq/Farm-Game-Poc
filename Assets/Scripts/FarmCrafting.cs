using System;
using System.Collections.Generic;

public sealed class CraftRecipe
{
    public readonly string Name, Category, Description;
    public readonly FarmArt.Kind Icon;
    public readonly int[] Crops;
    public readonly int Coins, SalePrice;
    // Hauteur de l'illustration une fois posée dans la ferme ; 0 pour un objet qui ne se pose pas.
    public readonly float DecorHeight;
    public bool Placeable=>DecorHeight>0;
    // Crops : quantité requise par espèce, dans l'ordre de FarmSimulation.Crops.
    public CraftRecipe(string name,string category,string description,FarmArt.Kind icon,int carrots,int radishes,int pumpkins,int coins,int salePrice,float decorHeight=0)
    { Name=name;Category=category;Description=description;Icon=icon;Crops=new[]{carrots,radishes,pumpkins};Coins=coins;SalePrice=salePrice;DecorHeight=decorHeight; }
}

public static class FarmCrafting
{
    public const float CraftSeconds=2.4f;
    public static readonly CraftRecipe[] Recipes={
        new("Caisse de marché","Objet","Une caisse solide pour présenter tes légumes au marché.",FarmArt.Kind.Crate,0,2,0,3,16,.72f),
        new("Lanterne citrouille","Décoration","Une citrouille évidée qui éclaire doucement les soirées.",FarmArt.Kind.Pumpkin,0,1,1,0,30,.62f),
        new("Couronne fleurie","Accessoire","Une couronne tressée de fanes et de fleurs des champs.",FarmArt.Kind.Flowers,2,0,0,6,28),
        new("Engrais maison","Objet","Un mélange de fanes compostées pour nourrir la terre.",FarmArt.Kind.Sprout,1,2,0,0,22),
        new("Chapeau de roseaux","Accessoire","Un chapeau léger tressé avec les roseaux de la mare.",FarmArt.Kind.Reeds,0,3,0,5,26),
        new("Épouvantail","Décoration","Un gardien de paille au sourire de citrouille.",FarmArt.Kind.Farmer,2,0,1,6,52,1.5f)
    };
}

[Serializable]
public sealed class FarmDecor
{
    public int Kind,X,Z;
}

public sealed partial class FarmSimulation
{
    public const int MaxDecor=60;
    public readonly int[] Crafted=new int[FarmCrafting.Recipes.Length];
    public readonly List<FarmDecor> Decor=new();
    public event Action DecorChanged;
    public int DecorAt(int x,int z){for(int i=0;i<Decor.Count;i++)if(Decor[i].X==x&&Decor[i].Z==z)return i;return -1;}
    // Les décorations restent hors des champs et des parcelles, même non achetées.
    public static bool DecorCell(int x,int z)=>Inside(x,z)&&!Cultivable(x,z)&&FarmLand.At(x,z)<0;
    public bool CanPlaceDecor(int kind,int x,int z,int moving,out string message)
    {
        if(kind<0||kind>=FarmCrafting.Recipes.Length||!FarmCrafting.Recipes[kind].Placeable){message="Cet objet ne se pose pas dans la ferme.";return false;}
        if(!Inside(x,z)){message="Hors de la ferme.";return false;}
        if(!DecorCell(x,z)){message="Case réservée aux cultures.";return false;}
        int at=DecorAt(x,z);if(at>=0&&at!=moving){message="Case déjà occupée.";return false;}
        if(moving<0&&Crafted[kind]<1){message="Fabrique d'abord cet objet à l'établi.";return false;}
        if(moving<0&&Decor.Count>=MaxDecor){message="La ferme contient déjà "+MaxDecor+" décorations.";return false;}
        message="Emplacement libre.";return true;
    }
    public bool PlaceDecor(int kind,int x,int z,int moving,out string message)
    {
        if(!CanPlaceDecor(kind,x,z,moving,out message))return false;
        string name=FarmCrafting.Recipes[kind].Name;
        if(moving>=0&&moving<Decor.Count){Decor[moving].X=x;Decor[moving].Z=z;message=name+" déplacé(e).";}
        else{Crafted[kind]--;Decor.Add(new FarmDecor{Kind=kind,X=x,Z=z});message=name+" posé(e) dans la ferme.";}
        DecorChanged?.Invoke();return true;
    }
    public bool RemoveDecor(int index,out string message)
    {
        if(index<0||index>=Decor.Count){message="Choisis une décoration à retirer.";return false;}
        var item=Decor[index];Decor.RemoveAt(index);Crafted[item.Kind]++;DecorChanged?.Invoke();
        message=FarmCrafting.Recipes[item.Kind].Name+" rangé(e) dans l'inventaire.";return true;
    }
    public bool CanCraft(int recipe)
    {
        if(recipe<0||recipe>=FarmCrafting.Recipes.Length)return false;
        var r=FarmCrafting.Recipes[recipe];if(Money<r.Coins)return false;
        for(int i=0;i<Products.Length;i++)if(Products[i]<r.Crops[i])return false;
        return true;
    }
    public string MissingFor(int recipe)
    {
        var r=FarmCrafting.Recipes[recipe];var missing=new System.Collections.Generic.List<string>();
        for(int i=0;i<Products.Length;i++)if(Products[i]<r.Crops[i])missing.Add((r.Crops[i]-Products[i])+" × "+Crops[i].Name.ToLower());
        if(Money<r.Coins)missing.Add((r.Coins-Money)+" pièces");
        return string.Join(", ",missing);
    }
    public bool Craft(int recipe,out string message)
    {
        if(recipe<0||recipe>=FarmCrafting.Recipes.Length){message="Objet inconnu.";return false;}
        if(!CanCraft(recipe)){message="Il manque "+MissingFor(recipe)+".";return false;}
        var r=FarmCrafting.Recipes[recipe];
        for(int i=0;i<Products.Length;i++)Products[i]-=r.Crops[i];
        Money-=r.Coins;Crafted[recipe]++;
        message=r.Name+" fabriqué(e) ! Tu en possèdes "+Crafted[recipe]+".";return true;
    }
    public bool SellCrafted(int recipe,int amount,out string message)
    {
        if(recipe<0||recipe>=FarmCrafting.Recipes.Length||amount<1||amount>Crafted[recipe]){message="Tu n'as pas cette quantité dans ton inventaire.";return false;}
        var r=FarmCrafting.Recipes[recipe];int price=r.SalePrice*amount;Crafted[recipe]-=amount;Money+=price;
        message=amount+" × "+r.Name+" vendu(s) pour "+price+" pièces.";return true;
    }
    public int TotalCrafted { get { int sum=0;foreach(int count in Crafted)sum+=count;return sum; } }
}
