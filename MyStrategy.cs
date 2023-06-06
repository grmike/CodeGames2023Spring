using System;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

/**
 * Auto-generated code below aims at helping you parse
 * the standard input according to the problem statement.
 **/
class Player
{
    static void Main(string[] args)
    {
        string[] inputs;
        int numberOfCells = int.Parse(Console.ReadLine()); // amount of hexagonal cells in this map

        CellInfo[] cells = new CellInfo[numberOfCells];

        int CurrentTurn = 0;
        var mainSettings = new GameParams {
            HideOutputs = false
        };

        List<PathInfo> paths = new List<PathInfo>();

        for (int i = 0; i < numberOfCells; i++)
        {
            string lineText = Console.ReadLine();
            inputs = lineText.Split(' ');
            int type = int.Parse(inputs[0]); // 0 for empty, 1 for eggs, 2 for crystal
            int initialResources = int.Parse(inputs[1]); // the initial amount of eggs/crystals on this cell
            var neighbors = new List<int>();
            int neigh = int.Parse(inputs[2]); // the index of the neighbouring cell for each direction
            if (neigh >= 0) neighbors.Add(neigh);
            neigh = int.Parse(inputs[3]);
            if (neigh >= 0) neighbors.Add(neigh);
            neigh = int.Parse(inputs[4]);
            if (neigh >= 0) neighbors.Add(neigh);
            neigh = int.Parse(inputs[5]);
            if (neigh >= 0) neighbors.Add(neigh);
            neigh = int.Parse(inputs[6]);
            if (neigh >= 0) neighbors.Add(neigh);
            neigh = int.Parse(inputs[7]);
            if (neigh >= 0) neighbors.Add(neigh);

            cells[i] = new CellInfo
            {
                Index = i,
                Type = type,
                Resources = initialResources,
                Neighbors = neighbors.ToArray(),
                MyAnts = 0,
                OppAnts = 0
            };

            if (cells[i].Resources > 0) {
                paths.Add(new PathInfo {
                    Index = i,
                    Type = cells[i].Type,
                    Points = new List<PathPoint>(),
                });

                if (cells[i].Type == 1) {
                    mainSettings.PossibleAntsCount += cells[i].Resources;
                } else if (cells[i].Type == 2) {
                    mainSettings.PossibleGoldsCount += cells[i].Resources;
                }

                // Console.Error.WriteLine("Debug Cells[{0}] = {1} {2}", i, cells[i].Resources, 100);
            }
            
        }

        int numberOfBases = int.Parse(Console.ReadLine());
        mainSettings.Bases = new PathInfo[2*numberOfBases];
        mainSettings.MyBasesCount = numberOfBases;

        inputs = Console.ReadLine().Split(' ');
        for (int i = 0; i < numberOfBases; i++)
        {
            var myBaseIndex = int.Parse(inputs[i]);
            var myBase = new PathInfo {
                Index = myBaseIndex,
                Type = 0,
                IsMine = true,
                Points = new List<PathPoint>(),
            };
            paths.Add(myBase);
            mainSettings.Bases[i] = myBase;
            cells[myBaseIndex].IsMyBase = true;
        }

        inputs = Console.ReadLine().Split(' ');
        for (int i = 0; i < numberOfBases; i++)
        {
            int oppBaseIndex = int.Parse(inputs[i]);
            var oppBase = new PathInfo {
                Index = oppBaseIndex,
                Type = 0,
                IsMine = false,
                Points = new List<PathPoint>(),
            };
            paths.Add(oppBase);
            mainSettings.Bases[i + numberOfBases] = oppBase;
            cells[oppBaseIndex].IsOppBase = true;
        }

        foreach (var path in paths) {
            SetWeights(cells, path);
        }

        SetInfluence(paths, cells, mainSettings);


        var timer = new Stopwatch();

        // game loop
        while (true)
        {
            inputs = Console.ReadLine().Split(' ');
            int myScore = int.Parse(inputs[0]); 
            int oppScore = int.Parse(inputs[1]); 

            for (int i = 0; i < numberOfCells; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int resources = int.Parse(inputs[0]); // the current amount of eggs/crystals on this cell
                int myAnts = int.Parse(inputs[1]); // the amount of your ants on this cell
                int oppAnts = int.Parse(inputs[2]); // the amount of opponent ants on this cell

                if (cells[i].MyAnts != myAnts) 
                {
                    mainSettings.AllMyAntsCount += myAnts - cells[i].MyAnts;
                    cells[i].MyAnts = myAnts;
                }
                if (cells[i].OppAnts != oppAnts) 
                {
                    mainSettings.OppAntsCount += oppAnts - cells[i].OppAnts;
                    cells[i].OppAnts = oppAnts;
                }

                if (cells[i].Resources != resources) 
                {
                    // Console.Error.WriteLine("inputs[{0}] = {1}", cells[i].Index, string.Join(", ", inputs));

                    if (cells[i].Type == 2)
                    {
                        if (cells[i].OppAnts > cells[i].MyAnts) {
                            mainSettings.OppGoldsCount += cells[i].Resources - resources;
                        } else if (cells[i].OppAnts > 0 && cells[i].OppAnts == cells[i].MyAnts) {
                            mainSettings.OppGoldsCount += (cells[i].Resources - resources) / 2;
                        }
                    }

                    cells[i].Resources = resources;

                    if (cells[i].Resources == 0)
                    {
                        foreach(var item in paths) 
                        {
                            if (item.Index == i) {
                                paths.Remove(item);
                                break;
                            }
                        }

                        foreach(var path in paths) 
                        {
                            if (path.Points.Any(it => it.Index == i)) {
                                path.Points = new List<PathPoint>();
                                SetWeights(cells, path);
                            }
                        }
                    }
                }

            }

            if (CurrentTurn == 0) 
            {
                mainSettings.PossibleAntsCount += 2 * mainSettings.AllMyAntsCount; // добавляем изначальных своих и противника
            }

            List<Variant> variants = new List<Variant>();
            foreach (var basa in mainSettings.MyBases) {

                timer.Start();

                var unfiltered = GetVariants(paths, new Variant {
                    Weight = 0,
                    GameSettings = mainSettings,
                    Points = new List<VariantPath>{ new VariantPath {
                        Index = basa.Index,
                        Type = basa.Type,
                        Points = basa.Points
                    }}
                }, CurrentTurn);

                int freeAnts = mainSettings.MyBaseAntsCount;
                foreach(var variant in unfiltered)
                {
                    if (freeAnts >= variant.Weight && variant.Kf > 0) {
                        variants.Add(variant);
                        // Console.Error.WriteLine("Debug used variant = {0} - {1} - {2}", variant.Weight, variant.Kf, string.Join(", ", variant.Points.Select(it => it.Index)));
                        freeAnts -= variant.Weight; 
                    }
                }

                timer.Stop();

                if (!mainSettings.HideOutputs)
                {
                    Console.Error.WriteLine("GetVariant time = {0}", timer.ElapsedMilliseconds);
                    Console.Error.WriteLine("");
                }
                timer.Reset();
            }

            int maxStrength = variants.Any() ? variants.Select(it => it.Points.Count).Aggregate((x,y) => x * y) : 1;

            List<Action> result = new List<Action>();
            List<int> usedPoints = new List<int>();
            foreach(var variant in variants.OrderByDescending(it => it.Kf))
            {
                if (!mainSettings.IsEggsFirst) {

                    var points = variant.Points.Select(it => it.Index).ToList();
                    points.Remove(points.First());

                    var condition = usedPoints.Intersect(points).Count() < 1;
                    if (!condition) continue;
                    else usedPoints.AddRange(points);
                }

                // Console.Error.WriteLine("usedPoints = {0}", string.Join(", ", usedPoints.Select(it => it)));

                if (!mainSettings.HideOutputs)
                {
                    Console.Error.WriteLine("Used variant: Weight = {0}, Kf = {1}, Points = {2}", variant.Weight, variant.Kf, string.Join(", ", variant.Points.Select(it => it.Index)));
                }

                int from = -1;
                foreach(var item in variant.Points)
                {
                    if (from >= 0) {
                        result.Add(new Action
                        {
                            From = from,
                            To = item.Index,
                            Strength = 1 // maxStrength/(variant.Points.Count * variants.Count(it => it.Points.First().Index == variant.Points.First().Index))
                        });
                    }

                    from = item.Index;
                };
            }

            if (result.Any()) 
            {
                Console.WriteLine(string.Join("; ", result.Select(it => string.Format("LINE {0} {1} {2}", it.From, it.To, it.Strength)))
                     + "; MESSAGE " + mainSettings.GetCurrentMode()
                );
            }
            else 
            {
               Console.WriteLine("WAIT; MESSAGE WAIT"); 
            }
            
            mainSettings.WriteStatusToConsole();
            CurrentTurn++;
            // Console.Error.WriteLine("CurrentTurn = {0}", CurrentTurn);
        }
    }


    // Рассчитывем пути между точками
    static void SetWeights(CellInfo[] cells, PathInfo currentPath)
    {
        var queue = new Queue<CellInfoIterator>();
        queue.Enqueue(new CellInfoIterator {
            Index = currentPath.Index,
            Distance = 0
        });

        // Console.Error.WriteLine("Debug SetWeights currentPath = {0}", currentPath.Index);

        while (queue.Count > 0) 
        {
            var iterator = queue.Dequeue();
            var currentCell = cells[iterator.Index];
            if (!currentCell.Visited) {

                if (currentCell.Index != currentPath.Index
                    && (currentCell.Resources > 0 || currentCell.IsMyBase || currentCell.IsOppBase)) 
                {
                    // if (currentPath.Index == 31) {
                    //     Console.Error.WriteLine("Debug SetWeights = {0} {1}", iterator.Index, iterator.Distance);
                    // }

                    var exists = currentPath.Points.FirstOrDefault(it => it.Index == currentCell.Index);
                    if (exists == null) 
                    {
                        currentPath.Points.Add(new PathPoint{
                            Index = currentCell.Index,
                            Type = currentCell.Type,
                            Weight = iterator.Distance,
                        });
                    } else if (exists.Weight > iterator.Distance) {
                        exists.Weight = iterator.Distance;
                    }
                }
                else 
                {
                    foreach(int neight in currentCell.Neighbors)
                    {
                        var neightCell = cells[neight];
                        if (!neightCell.Visited) 
                        {
                            queue.Enqueue(new CellInfoIterator {
                                Index = neight,
                                Distance = iterator.Distance + 1
                            });
                        }
                    }
                }

                cells[iterator.Index].Visited = true;
            }
        }

        for (int i = 0; i < cells.Length ; i++)
        {
            cells[i].Visited = false;
        }
    }


    // Рассчитывем области влияния
    static void SetInfluence(List<PathInfo> paths, CellInfo[] cells, GameParams settings)
    {
        var queue = new Queue<PathInfo>();

        int nearestPoints = 0;
        int nearestDist = 0;
        int nearestGolds = 0;

        foreach(var antsBase in settings.Bases)
        {
            if (antsBase.IsMine) {
                antsBase.DistanceToMyBase = 0;
            } else {
                antsBase.DistanceToOppBase = 0;
            }

            queue.Enqueue(antsBase);
            while (queue.Count > 0) 
            {
                var item = queue.Dequeue();

                foreach(var neigh in item.Points)
                {
                    var path = paths.First(elm => elm.Index == neigh.Index);
                    if (antsBase.IsMine) {
                        if (path.DistanceToMyBase == -100 || path.DistanceToMyBase > neigh.Weight + item.DistanceToMyBase) 
                        {
                            path.DistanceToMyBase = neigh.Weight + item.DistanceToMyBase;
                            queue.Enqueue(path);

                            if (path.Type == 2 && path.DistanceToMyBase < 4 && !path.IsNearest)
                            {
                                nearestPoints += 1;
                                nearestDist += path.DistanceToMyBase;
                                nearestGolds += cells[path.Index].Resources;
                                path.IsNearest = true;
                                // Console.Error.WriteLine("Debug Nearest[{0}] = {1}, {2}, {3}", path.Index, nearestPoints, nearestDist, nearestGolds);
                            }
                        }
                    }
                    else 
                    {
                        if (path.DistanceToOppBase == -100 || path.DistanceToOppBase > neigh.Weight + item.DistanceToOppBase) 
                        {
                            path.DistanceToOppBase = neigh.Weight + item.DistanceToOppBase;
                            queue.Enqueue(path);
                        }
                    }
                }
            }
        }


        if (settings.PossibleGoldsCount / 2 <= nearestGolds) {
            settings.NearestGoldDistance = nearestDist;
            settings.NearestGoldPoints = nearestPoints;
        }
    }



    static List<Variant> GetVariants(List<PathInfo> paths, Variant currentVariant, int turn)
    {

        var queue = new Queue<Variant>();
        queue.Enqueue(currentVariant);

        // Console.Error.WriteLine("Debug SetWeights currentPath = {0}", currentPath.Index);

        int lookingCount = 6;

        List<Variant> result = new List<Variant>();
        while (queue.Count > 0) 
        {
            var variant = queue.Dequeue();

            if (variant.Points.Count < 7 && variant.Weight < variant.GameSettings.MyBaseAntsCount) 
            {
                var currentPath = variant.Points.Last();
                var effectivePoints = currentPath.Points.Where(it => it.Type != 0 
                    && !variant.Points.Any(elm => elm.Index == it.Index)).OrderBy(it => it.Weight)
                    .Take(variant.Points.Count > 3 ? 3 : lookingCount - variant.Points.Count);
                foreach(var path in effectivePoints)
                {
                    var effectivePath = paths.First(elm => elm.Index == path.Index);

                    // if (variant.Points.Any(it => it.Index == 20)) {
                    //     Console.Error.WriteLine("Debug Unusefull strategies = {0} - {1} - {2}", variant.Weight, variant.Kf, string.Join(", ", variant.Points.Select(it => it.Index)));
                    // }
                    if (effectivePath.Influence < -3) {
                        // отбрасываем неэффективные точки
                        // Console.Error.WriteLine("Debug Influence = {0}", effectivePath.Index);
                        continue;
                    }

                    var item = variant.Concat(path.Weight, effectivePath);
                    if (item.Kf > 0 && item.Kf < 1) 
                    {
                        // отбрасываем неэффективные стратегии
                        // Console.Error.WriteLine("Debug Unusefull strategies = {0} - {1} - {2}", item.Weight, item.Kf, string.Join(", ", item.Points.Select(it => it.Index)));
                        continue;
                    }
                    // if (variant.Points.First().Index == 47 && variant.Points.Any(it => it.Index == 33)) 
                    // {
                    //     Console.Error.WriteLine("Debug GetVariant = {0} - {1} - {2}", item.Kf, item.Weight, string.Join(", ", item.Points.Select(it => it.Index)));
                    // }

                    queue.Enqueue(item);
                    result.Add(item);
                }
            }
        }

        // Console.Error.WriteLine("Debug GetVariant Count = {0}", result.Count);
        // return result.OrderByDescending(it => it.Kf).Take(20).ToList();

        float bestKf = -1;

        List<Variant> variants = new List<Variant>();
        List<int> usedPoints = new List<int>();

        // if (currentVariant.GameSettings.IsEggsFirst)
        // {
        //     foreach(var variant in result.OrderByDescending(it => it.EggsPoints).ThenBy(it => it.)) {
        //     }

        //     return variants;
        // }

        foreach(var variant in result.OrderByDescending(it => it.Kf)) {

            // Console.Error.WriteLine("Debug variant = {0} - {1} - {2}", variant.Weight, variant.Kf, string.Join(", ", variant.Points.Select(it => it.Index)));
            if (bestKf == -1) 
            {
                // Console.Error.WriteLine("Debug bestKf = {0}", variant.Kf);
                bestKf = variant.Kf;
            }

            var points = variant.Points.Select(it => it.Index).ToList();
            points.Remove(points.First());

            // увеличиваем кол-во коротких путей в самом начале
            var condition = usedPoints.Intersect(points).Count() < 2 && variant.Kf * 3 > bestKf;

            // это условие показало свою эффективность на практике ?!
            // if (variant.GameSettings.MyBaseAntsCount > 20) {
            //     condition = points.Count() > 2 && usedPoints.Intersect(points).Count() < 2 && variant.Kf * 3 > bestKf;
            // }

            // Нацеливаемся на меньшее кол-во путей в эндшпиле, но болеее длинных ?!
            if (!variant.GameSettings.IsEggsFirst && variant.GameSettings.MyBaseAntsCount > 40) {
                condition = usedPoints.Intersect(points).Count() < 1 && variant.Kf * 5 > bestKf;
            }


            if (condition) 
            {
                // Console.Error.WriteLine("Debug possible variant = {0} - {1} - {2}", variant.Weight, variant.Kf, string.Join(", ", variant.Points.Select(it => it.Index)));

                usedPoints.AddRange(points);
                variants.Add(variant);
            }

        }

        return variants;
    }


    struct CellInfo 
    {
        public int Index;
        public int Type;
        public int Resources;
        public int[] Neighbors;
        public int MyAnts;
        public int OppAnts;
        public bool IsMyBase;
        public bool IsOppBase;
        // для обхода в ширину
        public bool Visited;
    }

    struct CellInfoIterator
    {
        public int Index;
        public int Distance;
    }

    // Точка с рассчитаными путями (обычно соответствует точке с ресурсами)
    class PathInfo 
    {
        public int Index;
        public int Type;
        public bool IsMine;
        public bool IsNearest;
        public int DistanceToMyBase = -100;
        public int DistanceToOppBase = -100;
        public List<PathPoint> Points;

        public int Influence { get { return DistanceToOppBase - DistanceToMyBase; } }
    }

    // Веса соседей
    class PathPoint 
    {
        public int Index;
        public int Type;
        public int Weight;
    }

    class VariantPath 
    {
        public int Index;
        public int Type;
        public List<PathPoint> Points;
    }

    class GameParams
    {
        public bool HideOutputs;

        public PathInfo[] Bases;
        public int MyBasesCount;
        public int AllMyAntsCount;
        public int PossibleAntsCount;
        public int PossibleGoldsCount;
        public int OppAntsCount;
        public int OppGoldsCount;

        public int NearestGoldPoints;
        public int NearestGoldDistance;

        public PathInfo[] MyBases { get {
            return Bases.Where(it => it.Type == 0 && it.IsMine).ToArray();
        }}

        public PathInfo[] OppBases { get {
            return Bases.Where(it => it.Type == 0 && !it.IsMine).ToArray();
        }}

        public int MyBaseAntsCount { get {
            return AllMyAntsCount / MyBasesCount;
        }}

        public bool IsEggsFirst { get {
            return PossibleAntsCount > 2 * AllMyAntsCount
                && 4 * OppGoldsCount < PossibleGoldsCount;
        }}

        public bool IsSpeedRun { get {
            return NearestGoldPoints > 0;
        }}

        public int SpeedRunKf { get {
            return AllMyAntsCount / NearestGoldDistance * NearestGoldPoints;
        }}

        public bool IsDangerMode { get {
            return 4 * OppGoldsCount >= PossibleGoldsCount;
        }}

        public void WriteStatusToConsole()
        {
            if (!HideOutputs) 
            {
                Console.Error.WriteLine("");
                Console.Error.WriteLine("PossibleAntsCount = {0}", PossibleAntsCount);
                Console.Error.WriteLine("AllMyAntsCount = {0}", AllMyAntsCount);
                Console.Error.WriteLine("BasesMyAntsCount = {0}", MyBaseAntsCount);
                Console.Error.WriteLine("OppAntsCount = {0}", OppAntsCount);
                Console.Error.WriteLine("OppGoldsCount = {0}", OppGoldsCount);
                Console.Error.WriteLine("PossibleGoldsCount = {0}", PossibleGoldsCount);
                Console.Error.WriteLine("NearestGoldPoints = {0}", NearestGoldPoints);
                Console.Error.WriteLine("NearestGoldDistance = {0}", NearestGoldDistance);
            }
        }

        public string GetCurrentMode()
        {
            if (HideOutputs) return "Fight";
            if (IsSpeedRun) return "SPEED RUN";
            if (IsEggsFirst) return "Eggs First";
            if (IsDangerMode) return "DANGER";
            return "Normal";
        }

    }

    class Variant 
    {
        public int Weight;
        public List<VariantPath> Points;
        public GameParams GameSettings;
        
        public float Kf { get { 

            if (GameSettings.IsDangerMode)
            {
                return (Weight == 0 || GoldsPoints < 1) ? 0 
                : GameSettings.MyBaseAntsCount * GoldsPoints / (float)Weight;
            }
            if (GameSettings.IsSpeedRun) 
            {
                if (Weight == 0 || Weight > 4 || Points.Count < 1) return 0;
                return GoldsPoints > 0 ? GameSettings.MyBaseAntsCount * GoldsPoints / (float)Weight
                    : GameSettings.MyBaseAntsCount * 5 * EggsPoints / (float)Weight; 
            }
            if (GameSettings.IsEggsFirst) 
            {
                return (Weight == 0 || EggsPoints < 1) ? 0 
                : GameSettings.MyBaseAntsCount * EggsPoints / (float)Weight;
            }

            return (Weight == 0 || Points.Count < 1) ? 0 
            : GameSettings.MyBaseAntsCount * Points.Count / (float)Weight; 
        }}

        public float EffectiveKf { get { 

            if (GameSettings.IsDangerMode)
            {
                return (Weight == 0 || GoldsPoints < 1) ? 0 
                : GameSettings.MyBaseAntsCount * (GoldsPoints + 1) / (float)Weight;  
            }
            if (GameSettings.IsSpeedRun) 
            {
                if (Weight == 0 || Weight > 4 || Points.Count < 1) return 0;
                return GoldsPoints > 0 ? GameSettings.MyBaseAntsCount * (GoldsPoints + 1) / (float)Weight
                    : GameSettings.MyBaseAntsCount * 5 * (EggsPoints + 1) / (float)Weight; 
            }
            if (GameSettings.IsEggsFirst) 
            {
                return (Weight == 0 || EggsPoints < 1) ? 0 
                : GameSettings.MyBaseAntsCount * (EggsPoints + 1) / (float)Weight;
            }

            return (Weight == 0 || Points.Count < 1) ? 0 
            : GameSettings.MyBaseAntsCount * (Points.Count + 1) / (float)Weight; 
        }}

        public int EggsPoints { get { return Points.Count(it => it.Type == 1); } }
        public bool HasEggsPoints { get { return Points.Any(it => it.Type == 1); } }
        public int GoldsPoints { get { return Points.Count(it => it.Type == 2); } }

        public Variant Concat(int weight, PathInfo point) {
            var points = new List<VariantPath>(Points);
            points.Add(new VariantPath {
                Index = point.Index,
                Type = point.Type,
                Points = point.Points
            });
            return new Variant {
                Weight = Weight + weight,
                GameSettings = GameSettings,
                Points = points
            };
        }
    }

    struct Action 
    {
        public int From;
        public int To;
        public int Strength;
    }

}
