using KisanMitraAI.Core.Offline;
using Microsoft.Extensions.Logging;
using System.Net.NetworkInformation;

namespace KisanMitraAI.Infrastructure.Offline;

/// <summary>
/// Implementation of connectivity monitor
/// </summary>
public class ConnectivityMonitor : IConnectivityMonitor, IDisposable
{
    private readonly ILogger<ConnectivityMonitor> _logger;
    private readonly List<IObserver<ConnectivityStatus>> _observers;
    private readonly Timer _checkTimer;
    private ConnectivityStatus _currentStatus;
    private readonly HashSet<string> _operationsRequiringConnectivity;
    private readonly object _lock = new object();

    public ConnectivityMonitor(ILogger<ConnectivityMonitor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _observers = new List<IObserver<ConnectivityStatus>>();
        
        // Initialize with current status
        _currentStatus = new ConnectivityStatus(
            IsOnline: CheckNetworkConnectivity(),
            LastCheckedAt: DateTimeOffset.UtcNow);

        // Operations that require network connectivity
        _operationsRequiringConnectivity = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "VoiceQuery",
            "ImageUpload",
            "DocumentUpload",
            "PriceRetrieval",
            "WeatherForecast",
            "KnowledgeBaseQuery",
            "PlantingRecommendation"
        };

        // Check connectivity every 10 seconds
        _checkTimer = new Timer(
            CheckConnectivityCallback,
            null,
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(10));

        _logger.LogInformation("Connectivity monitor initialized. Initial status: {IsOnline}", _currentStatus.IsOnline);
    }

    public Task<bool> IsOnlineAsync(CancellationToken cancellationToken = default)
    {
        var isOnline = CheckNetworkConnectivity();
        return Task.FromResult(isOnline);
    }

    public Task<ConnectivityStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_currentStatus);
    }

    public IObservable<ConnectivityStatus> ObserveConnectivityChanges()
    {
        return new ConnectivityObservable(this);
    }

    public bool RequiresConnectivity(string operationType)
    {
        if (string.IsNullOrWhiteSpace(operationType))
            return true; // Default to requiring connectivity

        return _operationsRequiringConnectivity.Contains(operationType);
    }

    private void CheckConnectivityCallback(object? state)
    {
        try
        {
            var isOnline = CheckNetworkConnectivity();
            var newStatus = new ConnectivityStatus(
                IsOnline: isOnline,
                LastCheckedAt: DateTimeOffset.UtcNow,
                Message: isOnline ? "Connected" : "Offline");

            // Only notify if status changed
            if (newStatus.IsOnline != _currentStatus.IsOnline)
            {
                _logger.LogInformation(
                    "Connectivity status changed: {OldStatus} -> {NewStatus}",
                    _currentStatus.IsOnline ? "Online" : "Offline",
                    newStatus.IsOnline ? "Online" : "Offline");

                _currentStatus = newStatus;
                NotifyObservers(newStatus);
            }
            else
            {
                _currentStatus = newStatus;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking connectivity");
        }
    }

    private void NotifyObservers(ConnectivityStatus status)
    {
        lock (_lock)
        {
            foreach (var observer in _observers.ToList())
            {
                try
                {
                    observer.OnNext(status);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error notifying observer");
                }
            }
        }
    }

    private bool CheckNetworkConnectivity()
    {
        try
        {
            // Check if any network interface is up and operational
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            
            var hasActiveInterface = networkInterfaces.Any(ni =>
                ni.OperationalStatus == OperationalStatus.Up &&
                ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel);

            if (!hasActiveInterface)
            {
                return false;
            }

            // Try to ping a reliable host (Google DNS)
            using var ping = new Ping();
            try
            {
                var reply = ping.Send("8.8.8.8", timeout: 3000);
                return reply.Status == IPStatus.Success;
            }
            catch
            {
                // If ping fails, still consider online if interface is up
                // (might be behind firewall that blocks ICMP)
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking network connectivity, assuming offline");
            return false;
        }
    }

    internal void Subscribe(IObserver<ConnectivityStatus> observer)
    {
        lock (_lock)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
                // Send current status immediately
                observer.OnNext(_currentStatus);
            }
        }
    }

    internal void Unsubscribe(IObserver<ConnectivityStatus> observer)
    {
        lock (_lock)
        {
            _observers.Remove(observer);
        }
    }

    public void Dispose()
    {
        _checkTimer?.Dispose();
        
        lock (_lock)
        {
            foreach (var observer in _observers.ToList())
            {
                try
                {
                    observer.OnCompleted();
                }
                catch { }
            }
            _observers.Clear();
        }
    }

    private class ConnectivityObservable : IObservable<ConnectivityStatus>
    {
        private readonly ConnectivityMonitor _monitor;

        public ConnectivityObservable(ConnectivityMonitor monitor)
        {
            _monitor = monitor;
        }

        public IDisposable Subscribe(IObserver<ConnectivityStatus> observer)
        {
            _monitor.Subscribe(observer);
            return new Unsubscriber(_monitor, observer);
        }

        private class Unsubscriber : IDisposable
        {
            private readonly ConnectivityMonitor _monitor;
            private readonly IObserver<ConnectivityStatus> _observer;

            public Unsubscriber(ConnectivityMonitor monitor, IObserver<ConnectivityStatus> observer)
            {
                _monitor = monitor;
                _observer = observer;
            }

            public void Dispose()
            {
                _monitor.Unsubscribe(_observer);
            }
        }
    }
}
