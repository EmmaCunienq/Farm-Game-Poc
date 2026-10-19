using System;

public sealed class CraftRecipe
{
    public readonly string Name, Category, Description;
    public readonly FarmArt.Kind Icon;
    public readonly int[] Crops;
    public readonly int Coins, SalePrice;
    // Crops : quantité requise par espèce, dans l'ordre de FarmSimulation.Crops.
    public CraftRecipe(string name,string category,string description,FarmArt.Kind icon,int carrots,int radishes,int pumpkins,int coins,int salePrice)
    { Name=name;Category=category;Description=description;Icon=icon;Crops=new[]{carrots,radishes,pumpkins};Coins=coins;SalePrice=salePrice; }
}

public static class FarmCrafting
{
    public const float CraftSeconds=2.4f;
    public static readonly CraftRecipe[] Recipes={
        new("Caisse de marché","Objet","Une caisse solide pour présenter tes légumes au marché.",FarmArt.Kind.Crate,0,2,0,3,16),
        new("Lanterne citrouille","Décoration","Une citrouille évidée qui éclaire doucement les soirées.",FarmArt.Kind.Pumpkin,0,1,1,0,30),
        new("Couronne fleurie","Accessoire","Une couronne tressée de fanes et de fleurs des champs.",FarmArt.Kind.Flowers,2,0,0,6,28),
        new("Engrais maison","Objet","Un mélange de fanes compostées pour nourrir la terre.",FarmArt.Kind.Sprout,1,2,0,0,22),
        new("Chapeau de roseaux","Accessoire","Un chapeau léger tressé avec les roseaux de la mare.",FarmArt.Kind.Reeds,0,3,0,5,26),
        new("Épouvantail","Décoration","Un gardien de paille au sourire de citrouille.",FarmArt.Kind.Farmer,2,0,1,6,52)
    };
}

public sealed partial class FarmSimulation
{
    public readonly int[] Crafted=new int[FarmCrafting.Recipes.Length];
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
