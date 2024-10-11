var Options;
(function (Options) {
    Options[Options["ECHO"] = 1] = "ECHO";
    Options[Options["MUDServerStatusProtocolVariable"] = 1] = "MUDServerStatusProtocolVariable";
    Options[Options["MUDServerStatusProtocolValue"] = 2] = "MUDServerStatusProtocolValue";
    Options[Options["SupressGoAhead"] = 3] = "SupressGoAhead";
    Options[Options["TelnetType"] = 24] = "TelnetType";
    Options[Options["MUDServerStatusProtocol"] = 70] = "MUDServerStatusProtocol";
    Options[Options["MUDSoundProtocol"] = 90] = "MUDSoundProtocol";
    Options[Options["MUDeXtensionProtocol"] = 91] = "MUDeXtensionProtocol";
    Options[Options["SubNegotiationEnd"] = 240] = "SubNegotiationEnd";
    Options[Options["GoAhead"] = 249] = "GoAhead";
    Options[Options["SubNegotiation"] = 250] = "SubNegotiation";
    Options[Options["WILL"] = 251] = "WILL";
    Options[Options["DO"] = 253] = "DO";
    Options[Options["WONT"] = 252] = "WONT";
    Options[Options["DONT"] = 254] = "DONT";
    Options[Options["InterpretAsCommand"] = 255] = "InterpretAsCommand";
})(Options || (Options = {}));
export class TelnetOption {
    constructor() {
        this.Enabled = false;
    }
}
export class EchoOption extends TelnetOption {
}
export class SupressGoAhead extends TelnetOption {
}
export class NegotiateResponse {
    constructor() {
        this.NewInput = new Uint8Array();
        this.Response = new Uint8Array();
    }
}
export class TelnetNegotiator {
    constructor() {
        this.Options = [];
        this.SupportedClientTypes = ["256COLOR", "VT100", "ANSI", "TRUECOLOR"];
        this.NegotiatedClientTypes = [];
        this.currentTypeIndex = -1;
        this.ClientNegotiateTelnetType = new Uint8Array([
            Options.InterpretAsCommand,
            Options.SubNegotiation,
            Options.TelnetType,
            0
        ]);
    }
    IsNegotiationRequired(input) {
        return input.includes(Options.InterpretAsCommand);
    }
    Negotiate(input) {
        const response = new NegotiateResponse();
        let i = 0;
        let lastIndex = 0;
        while (i < input.length) {
            if (input[i] === Options.InterpretAsCommand) {
                response.NewInput = this.concatUint8Arrays(response.NewInput, input.slice(lastIndex, i));
                i++; // Move past IAC
                if (i >= input.length)
                    break;
                const command = input[i];
                i++; // Move past command
                switch (command) {
                    case Options.DO:
                    case Options.DONT:
                    case Options.WILL:
                    case Options.WONT:
                        if (i >= input.length)
                            break;
                        const option = input[i];
                        i++; // Move past option
                        response.Response = this.concatUint8Arrays(response.Response, this.handleCommand(command, option));
                        break;
                    case Options.SubNegotiation:
                        const subNegResponse = this.handleSubNegotiation(input.slice(i));
                        response.Response = this.concatUint8Arrays(response.Response, subNegResponse);
                        i += this.findSubNegotiationEnd(input.slice(i)) + 1;
                        break;
                }
                lastIndex = i;
            }
            else {
                i++; // Move to next character if not IAC
            }
        }
        response.NewInput = this.concatUint8Arrays(response.NewInput, input.slice(lastIndex));
        return response;
    }
    handleCommand(command, option) {
        switch (option) {
            case Options.TelnetType:
                if (command === Options.DO || command === Options.WILL) {
                    return this.SendNextClientType();
                }
                break;
            // Add more cases for other options as needed
        }
        return new Uint8Array();
    }
    handleSubNegotiation(input) {
        if (input[0] === Options.TelnetType && input[1] === 1) {
            return this.SendNextClientType();
        }
        return new Uint8Array();
    }
    findSubNegotiationEnd(input) {
        for (let i = 0; i < input.length - 1; i++) {
            if (input[i] === Options.InterpretAsCommand &&
                input[i + 1] === Options.SubNegotiationEnd) {
                return i + 1;
            }
        }
        return input.length;
    }
    SendNextClientType() {
        this.currentTypeIndex = (this.currentTypeIndex + 1) % this.SupportedClientTypes.length;
        const clientType = this.SupportedClientTypes[this.currentTypeIndex];
        const clientTypeBytes = new TextEncoder().encode(clientType);
        return this.concatUint8Arrays(this.ClientNegotiateTelnetType, clientTypeBytes, new Uint8Array([Options.InterpretAsCommand, Options.SubNegotiationEnd]));
    }
    concatUint8Arrays(...arrays) {
        const totalLength = arrays.reduce((acc, value) => acc + value.length, 0);
        const result = new Uint8Array(totalLength);
        let offset = 0;
        for (const array of arrays) {
            result.set(array, offset);
            offset += array.length;
        }
        return result;
    }
}
