using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace CrimsonStainedLands.Connections;

public class TCPConnection : BaseConnection
{
    byte[] buffer = new byte[1024];

    public Encoding Encoding {get;set;}
    public ConnectionManager Manager { get; }
    public NetworkStream Stream {get;set;}

    public TCPConnection(ConnectionManager manager, Socket socket) : base(socket)
    {
        this.Manager = manager;
        this.Stream = new NetworkStream(socket);
        this.Stream.ReadTimeout = 1;
        this.Status = ConnectionStatus.Connected;
    }

    public override byte[] Read()
    {
        try 
        {
            //if(!this.Stream.DataAvailable)
            //    return null;
            try
            {
                if (Socket != null && Socket.Poll(1, SelectMode.SelectRead))
                {
                    int read = this.Stream.Read(buffer);

                    if (read > 0)
                    {
                        return buffer.Take(read).ToArray();
                    }
                    else
                    {
                        Cleanup();
                        return null;
                    }
                }
                else
                    return null;
            }
            catch(IOException ex) 
            { 
                if(ex.Message.Contains("was aborted "))
                {
                    Cleanup();
                }
                return null; 
            }
        }
        catch 
        {
            Cleanup();
            return null;
        }
    }

    public override int Write(byte[] data) 
    {
        try 
        {

            if(data.Length > 0)
            {
                if(data[0] == (byte) TelnetNegotiator.Options.InterpretAsCommand)
                {
                    //var written = this.Socket.Send(data);
                    this.Stream.Write(data);
                    return data.Length;
                }
                else
                {
                    //var written = this.Socket.Send(data);
                    this.Stream.Write(data);
                    return data.Length;
                }
            }
            else
                return 0;
        }
        catch
        {
            this.Status = ConnectionStatus.Disconnected;
            return 0;
        }
    }

    public override BaseConnection.ConnectionStatus Status
    {
        get;set;
    }

    public override void Cleanup()
    {
        this.Status = ConnectionStatus.Disconnected;
        this.Manager.Connections.Remove(this);
        try
        {
            this.Stream.Dispose();
        }
        catch 
        {

        }
        try
        {
            this.Socket.Dispose();
        }
        catch
        {
        }
    }
}