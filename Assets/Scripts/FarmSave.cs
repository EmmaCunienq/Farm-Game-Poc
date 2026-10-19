using System;
using System.IO;
using UnityEngine;

[Serializable]
public sealed class FarmSaveData
{
    public int Version=1,Day,Money,SelectedSeed,SelectedProduct,ActiveTool;
    public int UnlockedLand;
    public double Hour;
    public int[] Seeds,Products;
    public FarmPlot[] Plots;
    public FarmHome Home;
    public FarmAppearance Appearance;
    public bool Indoors;
    public Vector3 PlayerPosition;
    public Vector2 Facing;
    public string SavedAt;
    static bool Finite(double n)=>!double.IsNaN(n)&&!double.IsInfinity(n);
    public bool Valid()
    {
        if(Version!=1||Day<1||Day>1000000||!Finite(Hour)||Hour<0||Hour>=24||Money<0||SelectedSeed<0||SelectedSeed>2||SelectedProduct<0||SelectedProduct>2||ActiveTool<0||ActiveTool>5)return false;
        if(UnlockedLand<0||(UnlockedLand&~FarmLand.AllUnlocked)!=0)return false;
        if(Seeds==null||Products==null||Seeds.Length!=3||Products.Length!=3||Plots==null||Plots.Length!=FarmSimulation.Width*FarmSimulation.Height)return false;
        foreach(int count in Seeds)if(count<0||count>10000000)return false;
        foreach(int count in Products)if(count<0||count>10000000)return false;
        foreach(var p in Plots)
            if(p==null||p.Crop< -1||p.Crop>2||p.UsesLeft<0||p.UsesLeft>3||!Finite(p.GrowthHours)||p.GrowthHours<0||p.GrowthHours>60||!Finite(p.EmptyHours)||p.EmptyHours<0||p.WateredDay< -1||p.WateredDay>Day)return false;
        for(int i=0;i<Plots.Length;i++)
            if((Plots[i].Tilled||Plots[i].Crop>=0)&&!FarmLand.Cultivable(i%FarmSimulation.Width,i/FarmSimulation.Width,UnlockedLand))return false;
        if(Home==null||!Home.Valid()||Appearance==null||!Appearance.Valid())return false;
        if(!Finite(PlayerPosition.x)||!Finite(PlayerPosition.y)||!Finite(PlayerPosition.z)||!Finite(Facing.x)||!Finite(Facing.y)||Facing.sqrMagnitude<.5f||Facing.sqrMagnitude>1.5f)return false;
        return Indoors?PlayerPosition.x>=FarmWorld25D.HomeOffsetX+.3f&&PlayerPosition.x<=FarmWorld25D.HomeOffsetX+8.7f&&PlayerPosition.z>=.3f&&PlayerPosition.z<=6.7f:
            PlayerPosition.x>=.3f&&PlayerPosition.x<=14.7f&&PlayerPosition.z>=.3f&&PlayerPosition.z<=10.7f;
    }
}

public sealed class FarmSave
{
    public string Path { get; }
    public bool Exists=>File.Exists(Path);
    public FarmSave(string path=null){Path=path??System.IO.Path.Combine(Application.persistentDataPath,"farm-save.json");}
    public bool Write(FarmSaveData data,out string message)
    {
        try
        {
            if(data==null||!data.Valid())throw new InvalidDataException("L'état du jeu est invalide.");
            data.SavedAt=DateTime.UtcNow.ToString("o");
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            File.WriteAllText(Path+".tmp",JsonUtility.ToJson(data,true));
            // Replace est atomique : le dernier fichier complet reste disponible en cas d'interruption.
            if(File.Exists(Path))File.Replace(Path+".tmp",Path,Read(out _,out _)?Path+".bak":null);else File.Move(Path+".tmp",Path);
            message="Partie sauvegardée.";return true;
        }
        catch(Exception e) when(e is IOException||e is InvalidDataException||e is UnauthorizedAccessException||e is ArgumentException||e is NotSupportedException)
        {message="Sauvegarde impossible : "+e.Message;return false;}
    }
    public bool Read(out FarmSaveData data,out string message,bool backup=false)
    {
        data=null;
        try
        {
            string source=Path+(backup?".bak":"");
            if(!File.Exists(source)){message="Aucune sauvegarde disponible.";return false;}
            if(new FileInfo(source).Length>2000000)throw new InvalidDataException("Fichier trop volumineux.");
            var loaded=JsonUtility.FromJson<FarmSaveData>(File.ReadAllText(source));
            if(loaded==null||!loaded.Valid())throw new InvalidDataException("Fichier incompatible ou endommagé.");
            data=loaded;message=backup?"Copie de secours chargée.":"Partie chargée.";return true;
        }
        catch(Exception e) when(e is IOException||e is InvalidDataException||e is UnauthorizedAccessException||e is ArgumentException||e is NotSupportedException)
        {message="Chargement impossible : "+e.Message;return false;}
    }
}
