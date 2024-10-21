// app.ts
import { WebSocketManager } from './websocket';
import { ANSITextColorizer } from './color';
const outputDiv = document.getElementById('output');
const inputField = document.getElementById('input');
const wsManager = new WebSocketManager(appendToOutput, (message) => { }, //appendToOutput(`> ${message}\n`),
() => appendToOutput('Connected successfully\n'));
const textColorizer = new ANSITextColorizer();
// Command history functionality
const maxHistoryLength = 20;
let commandHistory = [];
let currentHistoryIndex = -1;
// Keymap for directional commands
const keyMap = {
    'Numpad8': 'north',
    'Numpad6': 'east',
    'Numpad2': 'south',
    'Numpad4': 'west',
    'Numpad9': 'up',
    'Numpad3': 'down',
    'Numpad5': 'look',
    'Escape': '/selectinput'
};
function escapeHTML(unsafeText) {
    return unsafeText
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}
function appendToOutput(message) {
    const processedMessage = escapeHTML(message)
        .replace(/\n/g, '<br>')
        .replace(/ /g, '&nbsp;')
        .replace(/\t/g, '&nbsp;&nbsp;&nbsp;&nbsp;');
    const colorizedMessage = textColorizer.ColorText(processedMessage);
    outputDiv.innerHTML += colorizedMessage;
    outputDiv.scrollTop = outputDiv.scrollHeight;
}
function addToHistory(command) {
    if (command.trim() !== '' && (commandHistory.length == 0 || commandHistory[0] != command)) {
        commandHistory.unshift(command);
        if (commandHistory.length > maxHistoryLength) {
            commandHistory.pop();
        }
        currentHistoryIndex = -1;
    }
}
function navigateHistory(direction) {
    if (direction === 'up' && currentHistoryIndex < commandHistory.length - 1) {
        currentHistoryIndex++;
    }
    else if (direction === 'down' && currentHistoryIndex > -1) {
        currentHistoryIndex--;
    }
    if (currentHistoryIndex === -1) {
        inputField.value = '';
    }
    else {
        inputField.value = commandHistory[currentHistoryIndex];
    }
    // Move cursor to the end of the input
    setTimeout(() => {
        inputField.selectionStart = inputField.selectionEnd = inputField.value.length;
    }, 0);
}
function sendCommand(command) {
    if (command.toLowerCase() === '/connect') {
        if(wsManager.socket)
        {
            console.log("Closing connection");
            wsManager.socket.close();
        }
        wsManager.connect();
    }
    else if (command.toLowerCase() == "/selectinput") {
        inputField.select();
    }
    else if (wsManager.isConnected()) {
        wsManager.sendMessage(command);
        appendToOutput(`${command}\n`);
    }
    else {
        appendToOutput('Not connected. Type /connect to connect to the MUD server.\n');
    }
}
// Event listener for keydown events (handles both regular input and mapped keys)
document.addEventListener('keydown', function (e) {
    // Check if the pressed key is in our keyMap
    if (e.code in keyMap) {
        e.preventDefault(); // Prevent the key from being entered in the input field
        const command = keyMap[e.code];
        sendCommand(command);
        return;
    }
    // Handle arrow keys for command history
    if (e.key === 'ArrowUp' || e.key === 'ArrowDown') {
        e.preventDefault();
        navigateHistory(e.key === 'ArrowUp' ? 'up' : 'down');
        return;
    }
    // If the input field is not focused, focus it
    if (document.activeElement !== inputField) {
        inputField.focus();
    }
});
// Event listener for keypress events (handles Enter key for sending commands)
inputField.addEventListener('keypress', function (e) {
    if (e.key === 'Enter') {
        const command = inputField.value;
        addToHistory(command);
        sendCommand(command);
        inputField.select();
    }
});
appendToOutput('Welcome to the Web MUD Client\n');
appendToOutput('Type /connect to connect to the MUD server\n');
appendToOutput('Use numpad keys for quick movement:\n');
appendToOutput('8: north, 6: east, 2: south, 4: west, 9: up, 3: down\n');
