using System;
using System.Diagnostics;
using System.IO.Ports;

namespace QuotingEngine.Infrastructure.Hardware;

public class SerialPortReader : IDisposable
{
    private readonly SerialPort _serialPort;
    private bool _disposed;

    /// <summary>
    /// Indicates whether the serial port is currently open and connected.
    /// </summary>
    public bool IsConnected => !_disposed && _serialPort.IsOpen;

    /// <summary>
    /// The name of the configured serial port (e.g., "COM1").
    /// </summary>
    public string PortName => _serialPort.PortName;

    public event EventHandler<string>? DataReceived;
    public event EventHandler<string>? ConnectionStatusChanged;

    public SerialPortReader(string portName, int baudRate = 9600)
    {
        _serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
        _serialPort.DataReceived += SerialPort_DataReceived;
    }

    /// <summary>
    /// Attempts to open the serial port. Does NOT throw if the port is absent — 
    /// the app must be able to start without hardware attached.
    /// </summary>
    public void Start()
    {
        if (_disposed) return;

        try
        {
            if (!_serialPort.IsOpen)
            {
                _serialPort.Open();
                ConnectionStatusChanged?.Invoke(this, $"Connected to {_serialPort.PortName}");
                Debug.WriteLine($"[SerialPortReader] Opened {_serialPort.PortName} at {_serialPort.BaudRate} baud.");
            }
        }
        catch (Exception ex)
        {
            // Port not found, access denied, or other hardware issue.
            // This is expected when no scale/XRF is connected — the app must still start.
            Debug.WriteLine($"[SerialPortReader] Could not open {_serialPort.PortName}: {ex.Message}");
            ConnectionStatusChanged?.Invoke(this, $"Disconnected ({_serialPort.PortName} unavailable)");
        }
    }

    /// <summary>
    /// Attempts to reconnect to the serial port. Useful when hardware is plugged in after app start.
    /// </summary>
    public bool TryReconnect()
    {
        if (_disposed) return false;

        try
        {
            if (_serialPort.IsOpen) return true;

            _serialPort.Open();
            ConnectionStatusChanged?.Invoke(this, $"Connected to {_serialPort.PortName}");
            Debug.WriteLine($"[SerialPortReader] Reconnected to {_serialPort.PortName}.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SerialPortReader] Reconnect failed for {_serialPort.PortName}: {ex.Message}");
            ConnectionStatusChanged?.Invoke(this, $"Reconnect failed ({_serialPort.PortName})");
            return false;
        }
    }

    public void Stop()
    {
        try
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
                ConnectionStatusChanged?.Invoke(this, $"Disconnected from {_serialPort.PortName}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SerialPortReader] Error closing {_serialPort.PortName}: {ex.Message}");
        }
    }

    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            // Read until newline (\n) as required by PRD for hardware end-character
            var data = _serialPort.ReadLine();
            DataReceived?.Invoke(this, data.Trim());
        }
        catch (Exception ex)
        {
            // Ignore timeout/read exceptions — hardware can be noisy
            Debug.WriteLine($"[SerialPortReader] Read error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _serialPort.Dispose();
    }
}
