using System;

public enum FarmTool { Hoe, Seeds, Water, Basket, House, Wardrobe }
public enum CropKind { Carrot, Radish, Pumpkin }
public enum PlantStage { Empty, Seed, Sprout, Mature }

public sealed class CropDefinition
{
    public readonly string Name;
    public readonly double GrowthHours;
    public readonly int SeedPrice, SalePrice, Yield;
    public CropDefinition(string name,double hours,int seedPrice,int salePrice,int yield)
    { Name=name;GrowthHours=hours;SeedPrice=seedPrice;SalePrice=salePrice;Yield=yield; }
}

[Serializable]
public sealed class FarmPlot
{
    public bool Tilled;
    public int UsesLeft;
    public double EmptyHours, GrowthHours;
    public int Crop=-1, WateredDay=-1;
    public PlantStage Stage => Crop<0?PlantStage.Empty:
        GrowthHours>=FarmSimulation.Crops[Crop].GrowthHours?PlantStage.Mature:
        GrowthHours>=FarmSimulation.Crops[Crop].GrowthHours*.25?PlantStage.Sprout:PlantStage.Seed;
}

public sealed class FarmSimulation
{
    public const int Width=15, Height=11, HarvestsPerTilling=3;
    public const double RealSecondsPerDay=600, SoilIdleHours=48;
    public static readonly CropDefinition[] Crops={
        new CropDefinition("Carotte",36,2,8,2),
        new CropDefinition("Radis",24,1,5,3),
        new CropDefinition("Citrouille",60,5,18,1)
    };
    public readonly FarmPlot[,] Plots=new FarmPlot[Width,Height];
    public readonly int[] Seeds={5,3,2}, Products={0,0,0};
    public int Day { get; private set; }=1;
    public double Hour { get; private set; }=8;
    public int Money { get; private set; }=45;
    public int UnlockedLand { get; private set; }
    public int SelectedSeed, SelectedProduct;
    public event Action<int,int> PlotChanged;
    public event Action<int> NewDay;
    public event Action LandChanged;

    public FarmSimulation(bool demo=true)
    {
        for(int z=0;z<Height;z++)for(int x=0;x<Width;x++)Plots[x,z]=new FarmPlot();
        if(!demo)return;
        for(int z=3;z<7;z++)for(int x=4;x<9;x++) { var p=Plots[x,z];p.Tilled=true;p.UsesLeft=HarvestsPerTilling; }
        for(int i=0;i<3;i++){var p=Plots[4+i,6];p.Crop=i;p.GrowthHours=Crops[i].GrowthHours;}
    }
    public static bool Inside(int x,int z)=>x>=0&&z>=0&&x<Width&&z<Height;
    public static bool Cultivable(int x,int z)=>x>=3&&x<=9&&z>=2&&z<=6;
    public bool CanCultivate(int x,int z)=>FarmLand.Cultivable(x,z,UnlockedLand);
    public bool OwnsLand(int lot)=>lot>=0&&lot<FarmLand.Lots.Length&&(UnlockedLand&(1<<lot))!=0;
    public bool BuyLand(int lot,out string message)
    {
        if(lot<0||lot>=FarmLand.Lots.Length){message="Parcelle inconnue.";return false;}
        if(OwnsLand(lot)){message="Cette parcelle t'appartient déjà.";return false;}
        var definition=FarmLand.Lots[lot];
        if(Money<definition.Price){message="Il manque "+(definition.Price-Money)+" pièces pour cette parcelle.";return false;}
        Money-=definition.Price;UnlockedLand|=1<<lot;LandChanged?.Invoke();
        message=definition.Name+" achetée : "+definition.Cells+" nouvelles cases à labourer !";return true;
    }
    void Changed(int x,int z)=>PlotChanged?.Invoke(x,z);
    public bool Till(int x,int z,out string message)
    {
        if(!CanCultivate(x,z)){message=FarmLand.At(x,z)>=0?"Achète cette parcelle avant de la labourer.":"Cette terre n'est pas cultivable.";return false;}
        var p=Plots[x,z];if(p.Crop>=0){message="Cette case contient déjà une culture.";return false;}
        p.Tilled=true;p.UsesLeft=HarvestsPerTilling;p.EmptyHours=0;Changed(x,z);
        message="Terre préparée pour trois récoltes.";return true;
    }
    public bool Plant(int x,int z,out string message)
    {
        if(!Inside(x,z)||!CanCultivate(x,z)){message="Vise une case d'une parcelle qui t'appartient.";return false;}
        var p=Plots[x,z];if(!p.Tilled){message="Bêche cette case avant de planter.";return false;}
        if(p.Crop>=0){message="Cette case contient déjà une culture.";return false;}
        if(Seeds[SelectedSeed]<1){message="Plus de graines de "+Crops[SelectedSeed].Name.ToLower()+".";return false;}
        Seeds[SelectedSeed]--;p.Crop=SelectedSeed;p.GrowthHours=0;p.EmptyHours=0;p.WateredDay=-1;Changed(x,z);
        message=Crops[SelectedSeed].Name+" planté(e). L'arrosage accélère la pousse.";return true;
    }
    public bool Water(int x,int z,out string message)
    {
        if(!Inside(x,z)||Plots[x,z].Crop<0){message="Vise une culture pour l'arroser.";return false;}
        var p=Plots[x,z];if(p.Stage==PlantStage.Mature){message="Cette culture est prête à être récoltée.";return false;}
        if(p.WateredDay==Day){message="Cette culture a déjà été arrosée aujourd'hui.";return false;}
        p.WateredDay=Day;Changed(x,z);message="Arrosage terminé : pousse accélérée pour aujourd'hui.";return true;
    }
    public bool Harvest(int x,int z,out string message)
    {
        if(!Inside(x,z)||Plots[x,z].Stage!=PlantStage.Mature){message="Vise une culture mûre avec le panier.";return false;}
        var p=Plots[x,z];int kind=p.Crop;Products[kind]+=Crops[kind].Yield;SelectedProduct=kind;
        p.Crop=-1;p.GrowthHours=0;p.WateredDay=-1;p.EmptyHours=0;
        p.UsesLeft--;if(p.UsesLeft<=0)p.Tilled=false;
        Changed(x,z);message=Crops[kind].Yield+" × "+Crops[kind].Name+" ajouté(s) à l'inventaire.";
        if(!p.Tilled)message+=" La terre doit être bêchée à nouveau.";
        return true;
    }
    public bool BuySeeds(int kind,int amount,out string message)
    {
        if(kind<0||kind>=Crops.Length||amount<1||amount>999){message="Quantité incorrecte.";return false;}
        int price=Crops[kind].SeedPrice*amount;
        if(Money<price){message="Pas assez de pièces.";return false;}
        Money-=price;Seeds[kind]+=amount;message=amount+" graine(s) de "+Crops[kind].Name.ToLower()+" achetée(s).";return true;
    }
    public bool Sell(int kind,int amount,out string message)
    {
        if(kind<0||kind>=Crops.Length||amount<1||amount>Products[kind]){message="Tu n'as pas cette quantité dans ton inventaire.";return false;}
        int price=Crops[kind].SalePrice*amount;Products[kind]-=amount;Money+=price;
        message=amount+" × "+Crops[kind].Name+" vendu(s) pour "+price+" pièces.";return true;
    }
    public void AdvanceRealSeconds(double seconds)
    {
        if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<=0)return;
        AdvanceHours(seconds*24/RealSecondsPerDay);
    }
    void AdvanceHours(double hours)
    {
        while(hours>1e-9)
        {
            double step=Math.Min(hours,24-Hour);
            for(int z=0;z<Height;z++)for(int x=0;x<Width;x++)
            {
                var p=Plots[x,z];var oldStage=p.Stage;bool oldSoil=p.Tilled;
                if(p.Crop>=0)p.GrowthHours=Math.Min(Crops[p.Crop].GrowthHours,p.GrowthHours+step*(p.WateredDay==Day?1.35:1));
                else if(p.Tilled){p.EmptyHours+=step;if(p.EmptyHours>=SoilIdleHours){p.Tilled=false;p.UsesLeft=0;}}
                if(p.Stage!=oldStage||p.Tilled!=oldSoil)Changed(x,z);
            }
            Hour+=step;hours-=step;
            if(Hour>=24-1e-9)
            {
                Hour=0;Day++;
                for(int z=0;z<Height;z++)for(int x=0;x<Width;x++)if(Plots[x,z].WateredDay>=0)Changed(x,z);
                NewDay?.Invoke(Day);
            }
        }
    }
    public void SkipToNextMorning()=>AdvanceHours(24-Hour+8);
    public void Export(FarmSaveData save)
    {
        save.Day=Day;save.Hour=Hour;save.Money=Money;save.SelectedSeed=SelectedSeed;save.SelectedProduct=SelectedProduct;
        save.UnlockedLand=UnlockedLand;
        save.Seeds=(int[])Seeds.Clone();save.Products=(int[])Products.Clone();
        save.Plots=new FarmPlot[Width*Height];
        for(int z=0;z<Height;z++)for(int x=0;x<Width;x++)
        {
            var p=Plots[x,z];save.Plots[z*Width+x]=new FarmPlot{Tilled=p.Tilled,UsesLeft=p.UsesLeft,EmptyHours=p.EmptyHours,GrowthHours=p.GrowthHours,Crop=p.Crop,WateredDay=p.WateredDay};
        }
    }
    public void Restore(FarmSaveData save)
    {
        if(save==null||!save.Valid())throw new ArgumentException("Sauvegarde invalide.");
        Day=save.Day;Hour=save.Hour;Money=save.Money;SelectedSeed=save.SelectedSeed;SelectedProduct=save.SelectedProduct;
        UnlockedLand=save.UnlockedLand;LandChanged?.Invoke();
        Array.Copy(save.Seeds,Seeds,3);Array.Copy(save.Products,Products,3);
        for(int z=0;z<Height;z++)for(int x=0;x<Width;x++){var p=save.Plots[z*Width+x];Plots[x,z]=new FarmPlot{Tilled=p.Tilled,UsesLeft=p.UsesLeft,EmptyHours=p.EmptyHours,GrowthHours=p.GrowthHours,Crop=p.Crop,WateredDay=p.WateredDay};Changed(x,z);}
    }
    public int TotalProducts { get { int sum=0;foreach(int count in Products)sum+=count;return sum; } }
    public string ClockText { get { int minutes=(int)(Hour*60);return (minutes/60).ToString("00")+":"+(minutes%60).ToString("00"); } }
}
