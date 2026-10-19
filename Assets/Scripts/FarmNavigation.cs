using System;
using System.Collections.Generic;
using UnityEngine;

// Petite grille A* reconstruite à chaque ordre : les meubles déplacés sont pris en compte.
public sealed class FarmNavigation
{
    readonly Rect bounds;
    readonly float step;
    readonly int width,height;
    readonly bool[] walkable;
    readonly Func<Vector2,bool> canStand;
    readonly Func<Vector2,Vector2,bool> canTravel;
    public FarmNavigation(Rect bounds,Func<Vector2,bool> canStand,Func<Vector2,Vector2,bool> canTravel=null,float step=.4f)
    {
        this.bounds=bounds;this.step=step;this.canStand=canStand;
        this.canTravel=canTravel??SampleSegment;
        width=Mathf.FloorToInt(bounds.width/step)+1;height=Mathf.FloorToInt(bounds.height/step)+1;
        walkable=new bool[width*height];for(int i=0;i<walkable.Length;i++)walkable[i]=canStand(Position(i));
    }
    Vector2 Position(int index)=>new(bounds.xMin+(index%width)*step,bounds.yMin+(index/width)*step);
    bool SampleSegment(Vector2 from,Vector2 to)
    {
        int count=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(from,to)/(.2f*step)));
        for(int i=1;i<=count;i++)if(!canStand(Vector2.Lerp(from,to,i/(float)count)))return false;
        return true;
    }
    int Nearest(Vector2 position,bool fromStart)
    {
        int best=-1;float distance=1;
        for(int i=0;i<walkable.Length;i++)
        {
            if(!walkable[i])continue;float d=(Position(i)-position).sqrMagnitude;
            if(d<distance&&(fromStart?canTravel(position,Position(i)):canTravel(Position(i),position))){best=i;distance=d;}
        }
        return best;
    }
    public bool FindPath(Vector2 from,Vector2 to,out List<Vector2> path)
    {
        path=new List<Vector2>();
        if(to.x<bounds.xMin||to.x>bounds.xMax||to.y<bounds.yMin||to.y>bounds.yMax||!canStand(to))return false;
        if(canTravel(from,to)){path.Add(to);return true;}
        int start=Nearest(from,true),goal=Nearest(to,false);if(start<0||goal<0)return false;
        var costs=new float[walkable.Length];var parent=new int[walkable.Length];var closed=new bool[walkable.Length];var open=new List<int>{start};
        for(int i=0;i<costs.Length;i++){costs[i]=float.PositiveInfinity;parent[i]=-1;}costs[start]=0;
        while(open.Count>0)
        {
            int selected=0;float best=float.PositiveInfinity;
            for(int i=0;i<open.Count;i++){float score=costs[open[i]]+Vector2.Distance(Position(open[i]),Position(goal));if(score<best){best=score;selected=i;}}
            int current=open[selected];open.RemoveAt(selected);if(closed[current])continue;closed[current]=true;
            if(current==goal)
            {
                var raw=new List<Vector2>{to};for(int i=goal;i>=0;i=parent[i])raw.Add(Position(i));raw.Reverse();
                Vector2 anchor=from;int next=0;
                while(next<raw.Count)
                {
                    int farthest=next;for(int i=raw.Count-1;i>next;i--)if(canTravel(anchor,raw[i])){farthest=i;break;}
                    if(!canTravel(anchor,raw[farthest]))return false;
                    path.Add(raw[farthest]);anchor=raw[farthest];next=farthest+1;
                }
                return true;
            }
            int x=current%width,z=current/width;
            for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
            {
                if(dx==0&&dz==0)continue;int nx=x+dx,nz=z+dz;
                if(nx<0||nz<0||nx>=width||nz>=height)continue;int neighbor=nz*width+nx;
                if(!walkable[neighbor]||closed[neighbor])continue;
                if(dx!=0&&dz!=0&&(!walkable[z*width+nx]||!walkable[nz*width+x]))continue;
                if(!canTravel(Position(current),Position(neighbor)))continue;
                float cost=costs[current]+step*(dx!=0&&dz!=0?1.414214f:1);
                if(cost>=costs[neighbor])continue;costs[neighbor]=cost;parent[neighbor]=current;open.Add(neighbor);
            }
        }
        return false;
    }
}
