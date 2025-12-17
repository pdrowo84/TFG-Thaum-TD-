using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class TowerTargeting
{
    public enum TargetType
    {
        First,
        Last,
        Close
        
    }
    public static Enemy GetTarget(TowerBehaviour CurrentTower, TargetType TargetMethod)
    {
        Collider[] EnemiesInRange = Physics.OverlapSphere(CurrentTower.transform.position, CurrentTower.Range, CurrentTower.EnemiesLayer);
        NativeArray<EnemyData> EnemiesToCalulate = new NativeArray<EnemyData>(EnemiesInRange.Length, Allocator.TempJob);
        NativeArray<Vector3> NodePositions = new NativeArray<Vector3>(GameLoopManager.NodePositions, Allocator.TempJob);
        NativeArray<float> NodeDistances = new NativeArray<float>(GameLoopManager.NodeDistances, Allocator.TempJob);
        NativeArray<int> EnemyToIndex = new NativeArray<int>(new int[] { -1 }, Allocator.TempJob);
        int EnemyIndexToReturn = -1;


        for (int i = 0; i < EnemiesInRange.Length; i++)
        {
            Enemy CurrentEnemy = EnemiesInRange[i].transform.parent.GetComponent<Enemy>();
            int EnemyIndexInList = EntitySummoner.EnemiesInGame.FindIndex(x => x == CurrentEnemy);

            EnemiesToCalulate[i] = new EnemyData(CurrentEnemy.transform.position, CurrentEnemy.NodeIndex, CurrentEnemy.Health, EnemyIndexInList);
        }

        SearchForEnemy EnemySearchJob = new SearchForEnemy
        {
            _EnemiestoCalculate = EnemiesToCalulate,
            _NodePositions = NodePositions,
            _NodeDistances = NodeDistances,
            _EnemyToIndex = EnemyToIndex,
            TargetingType = (int)TargetMethod,
            TowerPosition = CurrentTower.transform.position,
        };

        switch ((int)TargetMethod)
        {
            case 0: //First
                EnemySearchJob.CompareValue = Mathf.Infinity;
                break;

            case 1: //Last
                EnemySearchJob.CompareValue = Mathf.NegativeInfinity;
                break;

            case 2: //Close

                goto case 0;

        }

        JobHandle dependency = new JobHandle();
        JobHandle SearchJobHandle = EnemySearchJob.Schedule();

        SearchJobHandle.Complete();

        if (EnemiesToCalulate.Length == 0 || EnemyToIndex[0] == -1)
        {
            EnemiesToCalulate.Dispose();
            NodePositions.Dispose();
            NodeDistances.Dispose();
            EnemyToIndex.Dispose();
            return null;
        }
        EnemyIndexToReturn = EnemiesToCalulate[EnemyToIndex[0]].EnemyIndex;

        EnemiesToCalulate.Dispose();
        NodePositions.Dispose();
        NodeDistances.Dispose();
        EnemyToIndex.Dispose();

        return EntitySummoner.EnemiesInGame[EnemyIndexToReturn];

    }

    struct EnemyData
    {
        public EnemyData(Vector3 position, int nodeindex, float hp, int enemyIndex)
        {
            EnemyPosition = position;
            NodeIndex = nodeindex;
            EnemyIndex = enemyIndex;
            Health = hp;
        }

        public Vector3 EnemyPosition;
        public int EnemyIndex;
        public int NodeIndex;
        public float Health;

    }
    struct SearchForEnemy : IJob
    {
        public NativeArray<EnemyData> _EnemiestoCalculate;
        public NativeArray<Vector3> _NodePositions;
        public NativeArray<float> _NodeDistances;
        public NativeArray<int> _EnemyToIndex;
        public Vector3 TowerPosition;
        public float CompareValue;
        public int TargetingType;

        public void Execute()
        {
            for (int index = 0; index < _EnemiestoCalculate.Length; index++)
            {
                float CurrentDistanceToEnd = 0;
                float DistanceToEnemy = 0;
                switch (TargetingType)
                {
                    case 0: //First
                        CurrentDistanceToEnd = GetDistanceToEnd(_EnemiestoCalculate[index]);
                        if (CurrentDistanceToEnd < CompareValue)
                        {
                            _EnemyToIndex[0] = index;
                            CompareValue = CurrentDistanceToEnd;
                        }
                        break;
                    case 1: //Last
                        CurrentDistanceToEnd = GetDistanceToEnd(_EnemiestoCalculate[index]);
                        if (CurrentDistanceToEnd > CompareValue)
                        {
                            _EnemyToIndex[0] = index;
                            CompareValue = CurrentDistanceToEnd;
                        }
                        break;
                    case 2: //Close
                        DistanceToEnemy = Vector3.Distance(TowerPosition, _EnemiestoCalculate[index].EnemyPosition);
                        if (DistanceToEnemy > CompareValue)
                        {
                            _EnemyToIndex[0] = index;
                            CompareValue = DistanceToEnemy;
                        }
                        break;
                }
            }
        }

        private float GetDistanceToEnd(EnemyData EnemyToEvaluate)
        {
            if (EnemyToEvaluate.NodeIndex < 0 || EnemyToEvaluate.NodeIndex >= _NodePositions.Length)
                return 0f;

            float FinalDistance = Vector3.Distance(EnemyToEvaluate.EnemyPosition, _NodePositions[EnemyToEvaluate.NodeIndex]);
            for (int i = EnemyToEvaluate.NodeIndex; i < _NodePositions.Length - 1 && i < _NodeDistances.Length; i++)
            {
                FinalDistance += _NodeDistances[i];
            }
            return FinalDistance;
        }
    }

}



