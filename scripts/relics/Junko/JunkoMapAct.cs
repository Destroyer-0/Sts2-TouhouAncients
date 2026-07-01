using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;

namespace TouhouAncients.Scripts.relics;

public class JunkoMapAct : ActMap
{
    private readonly MapPointType[] _defaultPointTypes =
    [
        MapPointType.Treasure,
        MapPointType.Elite,
        MapPointType.Unknown,
        MapPointType.RestSite,
        MapPointType.Elite,
        MapPointType.Treasure,
        MapPointType.Elite,
        MapPointType.Shop,
        MapPointType.Elite,
        MapPointType.Treasure,
        MapPointType.RestSite,
        MapPointType.Elite,
        MapPointType.Shop,
        MapPointType.Elite,
        MapPointType.RestSite
    ];

    private const int _width = 7;

    private const int _middle = 3;

    public override MapPoint BossMapPoint { get; }

    public override MapPoint StartingMapPoint { get; }
    public override MapPoint? SecondBossMapPoint { get; }

    protected override MapPoint?[,] Grid { get; }

    public JunkoMapAct(IRunState runState)
    {
        List<MapPointType> list = _defaultPointTypes.ToList();
        if (runState.Players.Count > 1)
        {
            list.RemoveAt(1);
        }

        var secondBoss = runState.AscensionLevel >= (int)AscensionLevel.DoubleBoss;
        Grid = new MapPoint[7, list.Count + (secondBoss? 2 : 1)];
        BossMapPoint = new MapPoint(GetColumnCount() / 2, GetRowCount())
        {
            PointType = MapPointType.Boss
        };
        StartingMapPoint = new MapPoint(GetColumnCount() / 2, 0)
        {
            PointType = MapPointType.Ancient
        };
        if (secondBoss)
        {
            SecondBossMapPoint = new MapPoint(GetColumnCount() / 2, GetRowCount() + 1)
            {
                PointType = MapPointType.Boss,
            };
        }
        else
        {
            SecondBossMapPoint = null;
        }

        for (int i = 0; i < list.Count; i++)
        {
            MapPoint mapPoint = new MapPoint(3, i + 1);
            Grid[3, i + 1] = mapPoint;
            mapPoint.PointType = list[i];
            if (i > 0)
            {
                Grid[3, i].AddChildPoint(mapPoint);
            }
        }

        startMapPoints.Add(Grid[3, 1]);
        Grid[3, GetRowCount() - (secondBoss ? 2 : 1)].AddChildPoint(BossMapPoint);
        if (SecondBossMapPoint != null)
        {
            BossMapPoint.AddChildPoint(SecondBossMapPoint);
        }

        StartingMapPoint.AddChildPoint(Grid[3, 1]);
    }
}