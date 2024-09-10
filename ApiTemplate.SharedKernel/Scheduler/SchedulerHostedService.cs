using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ApiTemplate.SharedKernel.Scheduler
{
    public class SchedulerHostedService : BackgroundService
    {
        private readonly ILogger<SchedulerHostedService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public SchedulerHostedService(ILogger<SchedulerHostedService> logger,
                                      IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        // expected execution of each task no more than 5 minets otherwise it will destroyed
        readonly TimeSpan TimeCancel = new(hours: 0, minutes: 5, seconds: 0);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!await WaitForAppStartup(_serviceProvider.GetRequiredService<IHostApplicationLifetime>(), stoppingToken))
                return;

            if (SchedulerExtension.StartWithMinuteBegin)
            {
                // wait 60sec + adjust time to ...:00 sec
                var sec = DateTime.UtcNow.Second;
                await Task.Delay(TimeSpan.FromSeconds(60 + 60 - sec), stoppingToken);
            }

            _ = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        var executeList = SchedulerExtension.TaskList.Where(x => x.CurrentTimeStart <= DateTime.UtcNow && !x.IsBusy && !x.StopExec).ToList();
                        foreach (var task in executeList)
                        {
                            using (var cts = new CancellationTokenSource(TimeCancel))
                            {
                                try
                                {
                                    using (var scope = _serviceProvider.CreateScope())
                                    {
                                        var schedulerTask = (scope.ServiceProvider.GetService(task.TaskType) as ISchedulerTask)!;
                                        if (task.CurrentTimeStart != DateTime.MinValue && !task.IsBusy && !task.StopExec)
                                        {
                                            task.IsBusy = true;
                                            await Task.Run(schedulerTask.Run, cts.Token);
                                        }
                                        task.CurrentTimeStart = schedulerTask.IncreaseTime();
                                    }
                                }
                                catch (OperationCanceledException)
                                {
                                    var ex = $"Task {task.TaskType.Name} was canceled by the elapsed time, {TimeCancel}: ";
                                    Debug.WriteLine(ex.ToString());
                                    _logger.LogError(ex.ToString());
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine(ex.ToString());
                                    _logger.LogError($"Task {task.TaskType.Name} have error: " + ex.ToString(), ex);
                                }
                                finally
                                {
                                    task.IsBusy = false;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                        try
                        {
                            _logger.LogError("Error from scheduler: " + ex.ToString());
                        }
                        catch { }
                    }
                    if (SchedulerExtension.StartWithMinuteBegin)
                    {
                        // adjust time to ...:00
                        var now = DateTime.UtcNow.Second;
                        await Task.Delay(TimeSpan.FromSeconds(60 - now), stoppingToken);
                    }
                }
            }, stoppingToken);
        }

        static async Task<bool> WaitForAppStartup(IHostApplicationLifetime lifetime, CancellationToken stoppingToken)
        {
            var startedSource = new TaskCompletionSource();
            using var reg1 = lifetime.ApplicationStarted.Register(startedSource.SetResult);

            var cancelledSource = new TaskCompletionSource();
            using var reg2 = stoppingToken.Register(cancelledSource.SetResult);

            Task completedTask = await Task.WhenAny(startedSource.Task, cancelledSource.Task).ConfigureAwait(false);
            return completedTask == startedSource.Task;
        }
    }
}
