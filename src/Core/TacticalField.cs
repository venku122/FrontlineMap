using System;
using System.Collections.Generic;
namespace FrontlineMap.Core
{
    public static class TacticalField
    {
        public static double Confidence(ObservationStamp stamp, double simulationTime, TacticalSettings settings)
        {
            if (!stamp.Stale) return 1;
            double age = Math.Max(0, simulationTime - stamp.ObservedTime);
            return Math.Pow(.5, age / settings.StaleHalfLife);
        }
        public static bool Profile(ObservationStamp stamp, TacticalSettings settings, out double radius, out double weight)
        {
            radius = 0; weight = stamp.Weight;
            switch (settings.Layer)
            {
                case TacticalLayer.GroundControl: case TacticalLayer.Confidence:
                    switch (stamp.Role)
                    {
                        case ObservationRole.GroundVehicle: case ObservationRole.AirDefense: radius = settings.GroundRadius; break;
                        case ObservationRole.Airbase: radius = settings.BaseRadius; break;
                        case ObservationRole.Building: radius = settings.BuildingRadius; weight *= settings.BuildingControlFactor; break;
                    }
                    break;
                case TacticalLayer.AirThreat:
                    if (stamp.Role == ObservationRole.Aircraft) radius = settings.AirRadius;
                    else if (stamp.Role == ObservationRole.AirDefense) radius = settings.AirDefenseRadius;
                    break;
                case TacticalLayer.NavalInfluence:
                    if (stamp.Role == ObservationRole.Ship) radius = settings.NavalRadius;
                    break;
            }
            return radius > 0 && weight > 0;
        }
        public static InfluenceBuffers Compute(ObservationSnapshot snapshot, ModelSettings settings)
        {
            if (settings.Tactical.Layer == TacticalLayer.Legacy) return ScalarField.Compute(snapshot, settings);
            var field = new InfluenceBuffers(snapshot.Geometry, true);
            foreach (var chunk in Calculate(snapshot, settings, field)) { }
            return field;
        }
        // Selected-layer calculation; bounded chunks and per-job scratch, never live Unity access.
        public static IEnumerable<int> Calculate(ObservationSnapshot snapshot, ModelSettings settings, InfluenceBuffers field)
        {
            var g = snapshot.Geometry; var t = settings.Tactical;
            bool paths = t.TerrainEnabled && t.Layer != TacticalLayer.AirThreat;
            var terrain = snapshot.Terrain;
            if (paths && terrain == null) yield break;
            var distances = paths ? new double[terrain.Geometry.Count] : null;
            int work = 0;
            foreach (var stamp in snapshot.Stamps)
            {
                if (!Profile(stamp, t, out double radius, out double weight)) continue;
                double confidence = Confidence(stamp, snapshot.SimulationTime, t);
                if (paths)
                    foreach (var chunk in terrain.ComputeDistances(stamp.X, stamp.Z, radius, t.Layer == TacticalLayer.NavalInfluence, distances))
                        yield return chunk;
                var target = stamp.Side == Side.Friendly ? field.Friendly : field.Enemy;
                var raw = stamp.Side == Side.Friendly ? field.RawFriendly : field.RawEnemy;
                // Rounded cell centers may coincide at extreme origins; retain the full-grid predicate there.
                bool fullGrid = Math.Max(Math.Abs(g.MinX), Math.Abs(stamp.X))/g.CellX >= 1e12
                    || Math.Max(Math.Abs(g.MinZ), Math.Abs(stamp.Z))/g.CellZ >= 1e12;
                int minX = fullGrid ? 0 : Bound(Math.Floor((stamp.X-radius-g.MinX)/g.CellX),g.Columns), maxX = fullGrid ? g.Columns-1 : Bound(Math.Ceiling((stamp.X+radius-g.MinX)/g.CellX),g.Columns);
                int minZ = fullGrid ? 0 : Bound(Math.Floor((stamp.Z-radius-g.MinZ)/g.CellZ),g.Rows), maxZ = fullGrid ? g.Rows-1 : Bound(Math.Ceiling((stamp.Z+radius-g.MinZ)/g.CellZ),g.Rows);
                double rr = radius*radius;
                for (int z=minZ;z<=maxZ;z++) for(int x=minX;x<=maxX;x++)
                {
                    int i=z*g.Columns+x;double px=g.X(x),pz=g.Z(z),dx=px-stamp.X,dz=pz-stamp.Z,d2=dx*dx+dz*dz;
                    if (paths)
                    {
                        int cell=terrain.CellIndex(px,pz);
                        if(cell<0) d2=double.PositiveInfinity;
                        else
                        {
                            var tg=terrain.Geometry;double tx=px-tg.X(cell%tg.Columns),tz=pz-tg.Z(cell/tg.Columns);
                            double path=distances[cell]+Math.Sqrt(tx*tx+tz*tz);d2=path*path;
                        }
                    }
                    if(d2<rr)
                    {
                        double contribution=weight*(1-d2/rr);raw[i]+=contribution;target[i]+=contribution*confidence;
                    }
                    if(++work==128){yield return work;work=0;}
                }
            }
            for(int i=0;i<g.Count;i++)
            {
                double total=field.RawFriendly[i]+field.RawEnemy[i];field.Confidence[i]=total>0 ? Math.Max(0,Math.Min(1,(field.Friendly[i]+field.Enemy[i])/total)) : 0;
                if(++work==128){yield return work;work=0;}
            }
            if(work>0)yield return work;
        }
        public static Pixel Sample(InfluenceBuffers field, ModelSettings settings, int index)
        {
            double f=field.Friendly[index],e=field.Enemy[index];
            if(Math.Max(f,e)<settings.SupportThreshold)return default(Pixel);
            double confidence=field.Confidence==null?1:field.Confidence[index];
            byte alpha=(byte)Math.Round(255*settings.Opacity*Math.Max(.15,confidence));
            if(settings.Tactical.Layer==TacticalLayer.Confidence)
                return new Pixel((byte)Math.Round(240*(1-confidence)),(byte)Math.Round(160+80*confidence),70,(byte)Math.Round(255*settings.Opacity),PixelKind.Confidence);
            if(f>=settings.SupportThreshold&&e>=settings.SupportThreshold&&Math.Abs(f-e)/(f+e)<=settings.Tactical.ContestedRatio)
                return new Pixel(177,130,215,alpha,PixelKind.Balanced);
            return f>e?new Pixel(65,155,245,alpha,PixelKind.Friendly):new Pixel(240,90,70,alpha,PixelKind.Enemy);
        }
        static int Bound(double value,int count)=>(int)Math.Max(0,Math.Min(count-1,value));
    }
}
