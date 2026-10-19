public sealed class FarmLandDefinition
{
    public readonly string Name;
    public readonly int Price,X,Z,Width,Depth;
    public int Cells=>Width*Depth;
    public FarmLandDefinition(string name,int price,int x,int z,int width,int depth)
    {Name=name;Price=price;X=x;Z=z;Width=width;Depth=depth;}
    public bool Contains(int x,int z)=>x>=X&&x<X+Width&&z>=Z&&z<Z+Depth;
}

public static class FarmLand
{
    public static readonly FarmLandDefinition[] Lots={
        new("Prairie du sud",30,3,0,7,2),
        new("Jardin de l'est",60,10,4,4,2),
        new("Lisière du nord",90,4,7,6,2)
    };
    public const int AllUnlocked=7;
    public static int At(int x,int z){for(int i=0;i<Lots.Length;i++)if(Lots[i].Contains(x,z))return i;return -1;}
    public static bool Cultivable(int x,int z,int unlocked)
    {
        if(FarmSimulation.Cultivable(x,z))return true;
        int lot=At(x,z);return lot>=0&&(unlocked&(1<<lot))!=0;
    }
}
