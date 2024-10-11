/**
 * ANSITextColorizer class
 * Converts ANSI color codes in text to HTML spans with inline styles.
 */
export class ANSITextColorizer {
    constructor() {
        // Current color and style states
        this.NewForegroundColor = "";
        this.NewBackgroundColor = "";
        this.NewIsBold = false;
        // Previous color and style states for comparison
        this.CurrentForegroundColor = "";
        this.CurrentBackgroundColor = "";
        this.CurrentIsBold = false;
        // Flag to track if we're currently within a styled span
        this.InStyle = false;
        // VGA color palette mapping ANSI color codes to RGB values
        this.PALETTE_VGA = {
            30: "rgb(0, 0, 0)", // Black
            31: "rgb(170, 0, 0)", // Red
            32: "rgb(0, 170, 0)", // Green
            33: "rgb(170, 85, 0)", // Yellow
            34: "rgb(0, 0, 170)", // Blue
            35: "rgb(170, 0, 170)", // Magenta
            36: "rgb(0, 170, 170)", // Cyan
            37: "rgb(170, 170, 170)", // White
            90: "rgb(85, 85, 85)", // Bright Black
            91: "rgb(255, 85, 85)", // Bright Red
            92: "rgb(85, 255, 85)", // Bright Green
            93: "rgb(255, 255, 85)", // Bright Yellow
            94: "rgb(85, 85, 255)", // Bright Blue
            95: "rgb(255, 85, 255)", // Bright Magenta
            96: "rgb(85, 255, 255)", // Bright Cyan
            97: "rgb(255, 255, 255)" // Bright White
        };
        // Extended color palette for 256 colors
        this.PALETTE_256 = [
            // 16 basic colors
            "#000000", "#800000", "#008000", "#808000", "#000080", "#800080", "#008080", "#c0c0c0",
            "#808080", "#ff0000", "#00ff00", "#ffff00", "#0000ff", "#ff00ff", "#00ffff", "#ffffff",
            // 216 RGB colors
            ...Array.from({ length: 216 }, (_, i) => {
                const r = Math.floor(i / 36) * 51;
                const g = Math.floor((i % 36) / 6) * 51;
                const b = (i % 6) * 51;
                return `rgb(${r},${g},${b})`;
            }),
            // 24 grayscale colors
            ...Array.from({ length: 24 }, (_, i) => {
                const v = 8 + i * 10;
                return `rgb(${v},${v},${v})`;
            })
        ];
    }
    ParseColorCode(text, startIndex) {
        const escapeSequence = text.substring(startIndex);
        let match;
        if ((match = escapeSequence.match(/^\[(3|4|7)z/))) {
            const [fullMatch] = match;
            return [startIndex + fullMatch.length, 0, false, true]; // Not bold, not base color
        }
        // 256 colors
        if ((match = escapeSequence.match(/^\[(38|48);5;(\d+)m/))) {
            const [fullMatch, base, colorId] = match;
            var color = parseInt(colorId);
            if (color > 8)
                color = color - 8 + 10;
            return [startIndex + fullMatch.length, color + 30, false, true]; // Not bold, not base color
        }
        // RGB colors
        if ((match = escapeSequence.match(/^\[(?:38|48);2;(\d+);(\d+);(\d+)m/))) {
            const [fullMatch, r, g, b] = match;
            return [startIndex + fullMatch.length, `rgb(${r},${g},${b})`, false, false]; // Not bold, not base color
        }
        // Standard ANSI colors
        if ((match = escapeSequence.match(/^\[(?:1;)?(\d+)m/))) {
            const [fullMatch, colorCode] = match;
            const isBold = fullMatch.includes('1;');
            const code = parseInt(colorCode);
            const isBaseColor = code == 0 || (code >= 30 && code <= 37) || (code >= 40 && code <= 47) ||
                (code >= 90 && code <= 97) || (code >= 100 && code <= 107);
            return [startIndex + fullMatch.length, code, isBold, isBaseColor];
        }
        // No match found
        return [startIndex, -1, false, false];
    }
    SetColor(colorCode, isBold, isBaseColor) {
        this.NewIsBold = isBold;
        if (typeof colorCode === 'string') {
            // RGB color
            this.NewForegroundColor = colorCode;
        }
        else if (typeof colorCode === 'number') {
            if (!isBaseColor && colorCode >= 0 && colorCode <= 255) {
                // 256 color palette
                this.NewForegroundColor = this.PALETTE_256[colorCode];
            }
            else if (colorCode >= 30 && colorCode <= 37) {
                // Standard foreground colors
                this.NewForegroundColor = this.PALETTE_VGA[colorCode];
            }
            else if (isBaseColor && colorCode >= 40 && colorCode <= 47) {
                // Standard background colors
                this.NewForegroundColor = this.PALETTE_VGA[colorCode + 50];
            }
            else if (!isBaseColor && colorCode >= 40 && colorCode <= 47) {
                // rgb background colors
                this.NewBackgroundColor = this.PALETTE_VGA[colorCode - 10];
            }
            else if (colorCode >= 90 && colorCode <= 97) {
                // Bright foreground colors
                this.NewForegroundColor = this.PALETTE_VGA[colorCode];
            }
            else if (colorCode >= 100 && colorCode <= 107) {
                // Bright background colors
                this.NewBackgroundColor = this.PALETTE_VGA[colorCode - 10];
            }
            else if (colorCode === 0) {
                // Reset all styles
                this.NewForegroundColor = "";
                this.NewBackgroundColor = "";
                this.NewIsBold = false;
            }
        }
    }
    /**
     * Appends new text to the existing text, adding style spans as necessary.
     * @param oldText The existing styled text
     * @param newText The new text to append
     * @returns The combined text with appropriate styling
     */
    AppendText(oldText, newText) {
        if (newText === "")
            return oldText;
        let spanCode = "";
        // Check if we need to change the current style
        if (this.NewBackgroundColor !== this.CurrentBackgroundColor ||
            this.NewForegroundColor !== this.CurrentForegroundColor ||
            this.NewIsBold !== this.CurrentIsBold) {
            // Close the previous style if there was one
            if (this.InStyle) {
                spanCode = "</span>";
            }
            // Open a new style span if needed
            if (this.NewBackgroundColor !== "" || this.NewForegroundColor !== "" || this.NewIsBold) {
                spanCode += "<span style='";
                if (this.NewBackgroundColor !== "") {
                    spanCode += `background-color: ${this.NewBackgroundColor};`;
                }
                if (this.NewForegroundColor !== "") {
                    spanCode += `color: ${this.NewForegroundColor};`;
                }
                if (this.NewIsBold) {
                    spanCode += `font-weight: bold;`;
                }
                spanCode += "'>";
                this.InStyle = true;
            }
            else {
                this.InStyle = false;
            }
            // Update the current style
            this.CurrentBackgroundColor = this.NewBackgroundColor;
            this.CurrentForegroundColor = this.NewForegroundColor;
            this.CurrentIsBold = this.NewIsBold;
        }
        return oldText + spanCode + newText;
    }
    /**
     * Converts text with ANSI color codes to HTML with inline styles.
     * @param text The input text with ANSI color codes
     * @returns The HTML string with color and style information
     */
    ColorText(text) {
        let newText = "";
        let index = 0;
        let lastIndex = 0;
        while (index > -1 && index < text.length) {
            // Find the next ANSI escape sequence
            index = text.indexOf('\x1b', index);
            if (index > -1) {
                // Append the text before the ANSI code
                newText = this.AppendText(newText, text.substring(lastIndex, index));
                // Parse and apply the color code
                const [newIndex, colorCode, isBold, isBaseColor] = this.ParseColorCode(text, index + 1);
                this.SetColor(colorCode, isBold, isBaseColor);
                index = newIndex;
            }
            else if (lastIndex < text.length) {
                // Append any remaining text
                newText = this.AppendText(newText, text.substring(lastIndex));
            }
            lastIndex = index;
        }
        // Close any open style span
        if (this.InStyle) {
            newText += '</span>';
        }
        return newText;
    }
}
