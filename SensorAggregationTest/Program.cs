﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using SensorActor.Interfaces;
using System.Diagnostics;
using FloorActor.Interfaces;




namespace SensorAggregationTest
{
    class Program: IActor
    {
        static Random mRand = new Random();
        static void SetTemperature(double average, double variation)
        {
            Task[] tasks = new Task[1000];
            Parallel.For(0, 1000, i =>
            {
               ISensorActor proxy = ActorProxy.Create<ISensorActor>(new ActorId(i), "fabric:/SensorAggregationApplication");
                tasks[i] = proxy.SetTemperatureAsync(average + (mRand.NextDouble() - 0.5) * 2 * variation);
            });

            Task.WaitAll(tasks);
        }
        static void SetIndexes()
        {
            Task[] tasks = new Task[1000];
            Parallel.For(0, 1000, i =>
            {
                var proxy = ActorProxy.Create<ISensorActor>(new ActorId(i), "fabric:/SensorAggregationApplication");
                tasks[i] = proxy.SetIndexAsync(i);
            });

            Task.WaitAll(tasks);
        }

        static void Main(string[] args)
        {
            SetIndexes();
            Stopwatch watch = new Stopwatch();
            watch.Start();
            SetTemperature(100, 50);
            watch.Stop();
            Console.WriteLine("Time to set temperature: " + watch.ElapsedMilliseconds);
            watch.Start();
            var proxy = ActorProxy.Create<IFloorActor>(new ActorId(2016), "fabric:/SensorAggregationApplication");
            Console.WriteLine("Average Temperature: " + proxy.GetTemperatureAsync().Result);
            watch.Stop();
            Console.WriteLine("Time to get average temperature: " + watch.ElapsedMilliseconds);

            Console.ReadKey();
        }
    }
}
