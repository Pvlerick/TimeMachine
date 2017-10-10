﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LiquidClock
{
    public sealed class TimeMachine
    {
        private readonly SortedDictionary<int, Action> actions = new SortedDictionary<int, Action>();

        /// <summary>
        /// Creates a <see cref="Task"/> of <typeparamref name="T"/> that will complete successfuly when the <see cref="TimeMachine"/> is advanced to the given <paramref name="time"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="time"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Task<T> ScheduleSuccess<T>(int time, T value) => AddAction<T>(time, tcs => tcs.SetResult(value));

        /// <summary>
        /// Creates a <see cref="Task"/> of <typeparamref name="T"/> that will fault when the <see cref="TimeMachine"/> is advanced to the given <paramref name="time"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="time"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        public Task<T> ScheduleFault<T>(int time, Exception exception) => AddAction<T>(time, tcs => tcs.SetException(exception));

        /// <summary>
        /// Creates a <see cref="Task"/> of <typeparamref name="T"/> that will fault when the <see cref="TimeMachine"/> is advanced to the given <paramref name="time"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="time"></param>
        /// <param name="exceptions"></param>
        /// <returns></returns>
        public Task<T> ScheduleFault<T>(int time, IEnumerable<Exception> exceptions) => AddAction<T>(time, tcs => tcs.SetException(exceptions));

        /// <summary>
        /// Creates a <see cref="Task"/> of <typeparamref name="T"/> that will be maked as cancelled when the <see cref="TimeMachine"/> is advanced to the given <paramref name="time"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="time"></param>
        /// <returns></returns>
        public Task<T> ScheduleCancellation<T>(int time) => AddAction<T>(time, tcs => tcs.SetCanceled());

        private Task<T> AddAction<T>(int time, Action<TaskCompletionSource<T>> action)
        {
            if (time <= 0)
                throw new ArgumentOutOfRangeException(nameof(time), "Tasks can only be scheduled with a positive time");

            if (actions.ContainsKey(time))
                throw new ArgumentException("A task completing at this time has already been scheduled.", nameof(time));

            TaskCompletionSource<T> source = new TaskCompletionSource<T>();
            actions[time] = () => action(source);

            return source.Task;
        }

        /// <summary>
        /// Execute the given action in the <see cref="TimeMachine"/>. Use the given <see cref="Advancer"/> to advance time and observe the tasks being completed/faulted/cancelled.
        /// </summary>
        /// <param name="action"></param>
        public void ExecuteInContext(Action<Advancer> action)
        {
            var temporaryContext = new ManuallyPumpedSynchronizationContext();

            SynchronizationContext originalContext = SynchronizationContext.Current;

            try
            {
                SynchronizationContext.SetSynchronizationContext(temporaryContext);
                Advancer advancer = new Advancer(actions, temporaryContext);
                // This is where the tests assertions etc will go...
                action(advancer);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(originalContext);
            }
        }
    }
}