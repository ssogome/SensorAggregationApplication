using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors.Client;
using System.Runtime.Serialization;
using FloorActor.Interfaces;
using SensorActor.Interfaces;
using System.ComponentModel;

namespace FloorActor
{
    /// <remarks>
    /// This class represents an actor.
    /// Every ActorID maps to an instance of this class.
    /// The StatePersistence attribute determines persistence and replication of actor state:
    ///  - Persisted: State is written to disk and replicated.
    ///  - Volatile: State is kept in memory only and replicated.
    ///  - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Persisted)]
    internal class FloorActor : Actor, IFloorActor
    {
        /// <summary>
        /// Initializes a new instance of FloorActor
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public FloorActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override Task OnActivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, "Actor activated.");

            // The StateManager is this actor's private state store.
            // Data stored in the StateManager will be replicated for high-availability for actors that use volatile or persisted state storage.
            // Any serializable object can be saved in the StateManager.
            // For more information, see https://aka.ms/servicefabricactorsstateserialization

            // return this.StateManager.TryAddStateAsync("count", 0);
            //return this.StateManager.TryAddStateAsync("myFloorState", new ActorState { Temperature = new double[1000] });
            return this.StateManager.TryAddStateAsync("myFloorState", new ActorState { Temperature = 0.0});
        }

        /// <summary>
        /// TODO: Replace with your own actor method.
        /// </summary>
        /// <returns></returns>
        Task<int> IFloorActor.GetCountAsync(CancellationToken cancellationToken)
        {
            return this.StateManager.GetStateAsync<int>("count", cancellationToken);
        }

        /// <summary>
        /// TODO: Replace with your own actor method.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        Task IFloorActor.SetCountAsync(int count, CancellationToken cancellationToken)
        {
            // Requests are not guaranteed to be processed in order nor at most once.
            // The update function here verifies that the incoming count is greater than the current count to preserve order.
            return this.StateManager.AddOrUpdateStateAsync("count", count, (key, value) => count > value ? count : value, cancellationToken);
        }
        //New code
        [DataContract]
        internal sealed class ActorState
        {
            //[DataMember]
            //public double[] Temperature { get; set; }
            [DataMember]
            public double Temperature { get; set; }
        }
        [ReadOnly(true)]
        async Task<double> IFloorActor.GetTemperatureAsync()
        {
            //ActorState state = await this.StateManager.GetStateAsync<ActorState>("myFloorState");
            //return state.Temperature.Average();
            Task<double>[] tasks = new Task<double>[1000];
            double[] readings = new double[1000];
            Parallel.For(0, 1000, i =>
            {
                var proxy = ActorProxy.Create<ISensorActor>(new ActorId(i), "fabric:/SensorAggregationApplication");
                tasks[i] = proxy.GetTemperatureAsync();
            });
            Task.WaitAll(tasks);
            Parallel.For(0, 1000, i =>
            {
                readings[i] = tasks[i].Result;
            });
            return await Task.FromResult(readings.Average());
        }
        //async Task IFloorActor.SetTemperatureAsync(int index, double temperature)
        //{
        //    ActorState state = await this.StateManager.GetStateAsync<ActorState>("myFloorState");
        //    state.Temperature[index] = temperature;
        //}
    }
}
