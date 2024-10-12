using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace CrimsonStainedLands.Connections;
public abstract class BaseConnection
{
    public enum ConnectionStatus 
    {
        Disconnected,
        Accepted,
        Authenticating,
        Negotiating,
        Connected
    }
    
    public BaseConnection(Socket socket) 
    {
        this.Socket = socket;
        this.RemoteEndPoint = socket.RemoteEndPoint;
        //this.Socket.Blocking = false;
        this.Status = ConnectionStatus.Accepted;
        this.Negotiator = new TelnetNegotiator();
        
    }

    public TelnetNegotiator Negotiator {get; private set;}
    
    public Socket Socket {get; set;}
    public Player Player {get; set;}

    public EndPoint RemoteEndPoint { get; set; }

    abstract public byte[] Read();
    abstract public int Write(byte[] data);
    
    abstract public ConnectionStatus Status { get; set; }

    abstract public void Cleanup();
    
}