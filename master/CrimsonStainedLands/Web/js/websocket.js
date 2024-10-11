// websocket.ts
import { TelnetNegotiator } from './telnet_negotiation';
/**
 * WebSocketManager class
 * Manages WebSocket connections for a MUD client
 */
export class WebSocketManager {
    /**
     * Constructor for WebSocketManager
     * @param outputHandler Function to handle output messages
     * @param inputHandler Function to handle input messages
     * @param onConnectHandler Function to call when connection is established
     */
    constructor(outputHandler, inputHandler, onConnectHandler) {
        this.negotiator = new TelnetNegotiator();
        // The WebSocket instance
        this.socket = null;
        this.outputHandler = outputHandler;
        this.inputHandler = inputHandler;
        this.onConnectHandler = onConnectHandler;
    }
    handleMessage(event) {
        if (event.data instanceof ArrayBuffer) {
            // Handle binary data
            const uint8Array = new Uint8Array(event.data);
            //console.log("Received binary data:", uint8Array);
            // Process the binary data
            this.processBinaryData(uint8Array);
        }
        else if (typeof event.data === "string") {
            // Handle text data
            //console.log("Received text data:", event.data);
            this.processTextData(event.data);
        }
        else {
            console.warn("Received unknown data type:", typeof event.data);
        }
    }
    processBinaryData(data) {
        if (this.negotiator.IsNegotiationRequired(data)) {
            const response = this.negotiator.Negotiate(data);
            if (response.Response.length > 0) {
                this.sendResponse(response.Response);
            }
            if (response.NewInput.length > 0) {
                this.outputHandler(new TextDecoder().decode(response.NewInput));
            }
        }
        else {
            this.outputHandler(new TextDecoder().decode(data));
        }
    }
    processTextData(data) {
        this.outputHandler(data);
    }
    sendResponse(response) {
        if (this.socket && this.socket.readyState === WebSocket.OPEN) {
            this.socket.send(response);
        }
    }
    /**
     * Establishes a WebSocket connection to the server
     */
    connect() {
        const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
        const host = window.location.host;
        this.socket = new WebSocket(`${protocol}//${host}`);
        this.socket.binaryType = "arraybuffer";
        this.socket.onmessage = this.handleMessage.bind(this);
        this.socket.onopen = (e) => {
            this.outputHandler('Connected to MUD server\n');
            this.onConnectHandler();
        };
        this.socket.onclose = (event) => {
            const message = event.wasClean
                ? `Connection closed cleanly, code=${event.code} reason=${event.reason}\n`
                : '\nConnection died\n';
            this.outputHandler(message);
        };
        this.socket.onerror = (error) => {
            this.outputHandler(`\nError: ${error.message}\n`);
        };
    }
    /**
     * Sends a message through the WebSocket connection
     * @param message The message to send as a string
     */
    sendMessage(message) {
        if (this.socket && this.socket.readyState === WebSocket.OPEN) {
            const uint8Array = new TextEncoder().encode(message + "\n");
            this.socket.send(uint8Array);
            this.inputHandler(message);
        }
        else {
            this.outputHandler('Not connected. Type /connect to connect to the MUD server.\n');
        }
    }
    /**
     * Checks if the WebSocket connection is currently open
     * @returns true if connected, false otherwise
     */
    isConnected() {
        return this.socket !== null && this.socket.readyState === WebSocket.OPEN;
    }
}
