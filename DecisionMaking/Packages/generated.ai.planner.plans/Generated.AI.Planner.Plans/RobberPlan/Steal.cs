using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.AI.Planner;
using Unity.AI.Planner.Traits;
using Unity.Burst;
using Generated.AI.Planner.StateRepresentation;
using Generated.AI.Planner.StateRepresentation.RobberPlan;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Generated.AI.Planner.Plans.RobberPlan
{
    [BurstCompile]
    struct Steal : IJobParallelForDefer
    {
        public Guid ActionGuid;
        
        const int k_parameter1Index = 0;
        const int k_parameter2Index = 1;
        const int k_MaxArguments = 2;

        public static readonly string[] parameterNames = {
            "parameter1",
            "parameter2",
        };

        [ReadOnly] NativeArray<StateEntityKey> m_StatesToExpand;
        StateDataContext m_StateDataContext;

        // local allocations
        [NativeDisableContainerSafetyRestriction] NativeArray<ComponentType> parameter1Filter;
        [NativeDisableContainerSafetyRestriction] NativeList<int> parameter1ObjectIndices;
        [NativeDisableContainerSafetyRestriction] NativeArray<ComponentType> parameter2Filter;
        [NativeDisableContainerSafetyRestriction] NativeList<int> parameter2ObjectIndices;

        [NativeDisableContainerSafetyRestriction] NativeList<ActionKey> ArgumentPermutations;
        [NativeDisableContainerSafetyRestriction] NativeList<StealFixupReference> TransitionInfo;

        bool LocalContainersInitialized => ArgumentPermutations.IsCreated;

        internal Steal(Guid guid, NativeList<StateEntityKey> statesToExpand, StateDataContext stateDataContext)
        {
            ActionGuid = guid;
            m_StatesToExpand = statesToExpand.AsDeferredJobArray();
            m_StateDataContext = stateDataContext;
            parameter1Filter = default;
            parameter1ObjectIndices = default;
            parameter2Filter = default;
            parameter2ObjectIndices = default;
            ArgumentPermutations = default;
            TransitionInfo = default;
        }

        void InitializeLocalContainers()
        {
            parameter1Filter = new NativeArray<ComponentType>(2, Allocator.Temp){[0] = ComponentType.ReadWrite<Robber>(),[1] = ComponentType.ReadWrite<Location>(),  };
            parameter1ObjectIndices = new NativeList<int>(2, Allocator.Temp);
            parameter2Filter = new NativeArray<ComponentType>(2, Allocator.Temp){[0] = ComponentType.ReadWrite<Treasure>(),[1] = ComponentType.ReadWrite<Location>(),  };
            parameter2ObjectIndices = new NativeList<int>(2, Allocator.Temp);

            ArgumentPermutations = new NativeList<ActionKey>(4, Allocator.Temp);
            TransitionInfo = new NativeList<StealFixupReference>(ArgumentPermutations.Length, Allocator.Temp);
        }

        public static int GetIndexForParameterName(string parameterName)
        {
            
            if (string.Equals(parameterName, "parameter1", StringComparison.OrdinalIgnoreCase))
                 return k_parameter1Index;
            if (string.Equals(parameterName, "parameter2", StringComparison.OrdinalIgnoreCase))
                 return k_parameter2Index;

            return -1;
        }

        void GenerateArgumentPermutations(StateData stateData, NativeList<ActionKey> argumentPermutations)
        {
            parameter1ObjectIndices.Clear();
            stateData.GetTraitBasedObjectIndices(parameter1ObjectIndices, parameter1Filter);
            
            parameter2ObjectIndices.Clear();
            stateData.GetTraitBasedObjectIndices(parameter2ObjectIndices, parameter2Filter);
            
            var RobberBuffer = stateData.RobberBuffer;
            
            

            for (int i0 = 0; i0 < parameter1ObjectIndices.Length; i0++)
            {
                var parameter1Index = parameter1ObjectIndices[i0];
                var parameter1Object = stateData.TraitBasedObjects[parameter1Index];
                
                if (!(RobberBuffer[parameter1Object.RobberIndex].Ready2Steal == true))
                    continue;
                
                
            
            

            for (int i1 = 0; i1 < parameter2ObjectIndices.Length; i1++)
            {
                var parameter2Index = parameter2ObjectIndices[i1];
                var parameter2Object = stateData.TraitBasedObjects[parameter2Index];
                
                
                

                var actionKey = new ActionKey(k_MaxArguments) {
                                                        ActionGuid = ActionGuid,
                                                       [k_parameter1Index] = parameter1Index,
                                                       [k_parameter2Index] = parameter2Index,
                                                    };
                argumentPermutations.Add(actionKey);
            
            }
            
            }
        }

        StateTransitionInfoPair<StateEntityKey, ActionKey, StateTransitionInfo> ApplyEffects(ActionKey action, StateEntityKey originalStateEntityKey)
        {
            var originalState = m_StateDataContext.GetStateData(originalStateEntityKey);
            var originalStateObjectBuffer = originalState.TraitBasedObjects;
            var originalparameter1Object = originalStateObjectBuffer[action[k_parameter1Index]];

            var newState = m_StateDataContext.CopyStateData(originalState);
            var newRobberBuffer = newState.RobberBuffer;
            {
                    var @Robber = newRobberBuffer[originalparameter1Object.RobberIndex];
                    @Robber.@Stolen = true;
                    newRobberBuffer[originalparameter1Object.RobberIndex] = @Robber;
            }

            
            newState.RemoveTraitBasedObjectAtIndex(action[k_parameter2Index]);

            var reward = Reward(originalState, action, newState);
            var StateTransitionInfo = new StateTransitionInfo { Probability = 1f, TransitionUtilityValue = reward };
            var resultingStateKey = m_StateDataContext.GetStateDataKey(newState);

            return new StateTransitionInfoPair<StateEntityKey, ActionKey, StateTransitionInfo>(originalStateEntityKey, action, resultingStateKey, StateTransitionInfo);
        }

        float Reward(StateData originalState, ActionKey action, StateData newState)
        {
            var reward = 0f;

            return reward;
        }

        public void Execute(int jobIndex)
        {
            if (!LocalContainersInitialized)
                InitializeLocalContainers();

            m_StateDataContext.JobIndex = jobIndex;

            var stateEntityKey = m_StatesToExpand[jobIndex];
            var stateData = m_StateDataContext.GetStateData(stateEntityKey);

            ArgumentPermutations.Clear();
            GenerateArgumentPermutations(stateData, ArgumentPermutations);

            TransitionInfo.Clear();
            TransitionInfo.Capacity = math.max(TransitionInfo.Capacity, ArgumentPermutations.Length);
            for (var i = 0; i < ArgumentPermutations.Length; i++)
            {
                TransitionInfo.Add(new StealFixupReference { TransitionInfo = ApplyEffects(ArgumentPermutations[i], stateEntityKey) });
            }

            // fixups
            var stateEntity = stateEntityKey.Entity;
            var fixupBuffer = m_StateDataContext.EntityCommandBuffer.AddBuffer<StealFixupReference>(jobIndex, stateEntity);
            fixupBuffer.CopyFrom(TransitionInfo);
        }

        
        public static T GetParameter1Trait<T>(StateData state, ActionKey action) where T : struct, ITrait
        {
            return state.GetTraitOnObjectAtIndex<T>(action[k_parameter1Index]);
        }
        
        public static T GetParameter2Trait<T>(StateData state, ActionKey action) where T : struct, ITrait
        {
            return state.GetTraitOnObjectAtIndex<T>(action[k_parameter2Index]);
        }
        
    }

    public struct StealFixupReference : IBufferElementData
    {
        internal StateTransitionInfoPair<StateEntityKey, ActionKey, StateTransitionInfo> TransitionInfo;
    }
}


