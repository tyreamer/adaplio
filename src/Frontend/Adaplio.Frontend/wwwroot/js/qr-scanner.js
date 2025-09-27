// QR Code Scanner functionality
let video = null;
let canvas = null;
let canvasContext = null;
let scanningTimer = null;
let stream = null;
let dotNetRef = null;

window.startQRScanner = async function (dotNetReference) {
    dotNetRef = dotNetReference;

    try {
        video = document.getElementById('qr-video');
        if (!video) {
            console.error('Video element not found');
            return;
        }

        // Request camera access
        stream = await navigator.mediaDevices.getUserMedia({
            video: {
                facingMode: 'environment', // Use back camera if available
                width: { ideal: 640 },
                height: { ideal: 480 }
            }
        });

        video.srcObject = stream;
        video.play();

        // Create canvas for image processing
        canvas = document.createElement('canvas');
        canvasContext = canvas.getContext('2d');

        // Start scanning when video is ready
        video.onloadedmetadata = () => {
            canvas.width = video.videoWidth;
            canvas.height = video.videoHeight;
            startScanning();
        };

    } catch (error) {
        console.error('Error accessing camera:', error);
        throw error;
    }
};

window.stopQRScanner = function () {
    if (scanningTimer) {
        clearInterval(scanningTimer);
        scanningTimer = null;
    }

    if (stream) {
        stream.getTracks().forEach(track => track.stop());
        stream = null;
    }

    if (video) {
        video.srcObject = null;
    }

    dotNetRef = null;
};

function startScanning() {
    scanningTimer = setInterval(() => {
        if (video && video.readyState === video.HAVE_ENOUGH_DATA) {
            scanFrame();
        }
    }, 500); // Scan every 500ms
}

function scanFrame() {
    try {
        // Draw current video frame to canvas
        canvasContext.drawImage(video, 0, 0, canvas.width, canvas.height);

        // Get image data
        const imageData = canvasContext.getImageData(0, 0, canvas.width, canvas.height);

        // Use jsQR library if available, otherwise use a simple pattern detection
        if (window.jsQR) {
            const code = jsQR(imageData.data, imageData.width, imageData.height);
            if (code && code.data) {
                handleQRCodeDetected(code.data);
                return;
            }
        }

        // Fallback: Simple URL pattern detection (for demo purposes)
        // In production, you'd want to use a proper QR code library like jsQR

    } catch (error) {
        console.error('Error scanning frame:', error);
    }
}

function handleQRCodeDetected(data) {
    console.log('QR Code detected:', data);

    if (dotNetRef && typeof dotNetRef.invokeMethodAsync === 'function') {
        dotNetRef.invokeMethodAsync('OnQRCodeDetected', data);
    }
}

// Simple QR pattern detection (fallback for when jsQR is not available)
// This is a very basic implementation - in production use jsQR library
function detectQRPattern(imageData) {
    // This is a placeholder for actual QR detection
    // In a real implementation, you would:
    // 1. Load jsQR library
    // 2. Use proper QR detection algorithms
    // 3. Handle different QR code formats

    return null;
}

// Load jsQR library dynamically if not already loaded
if (!window.jsQR) {
    const script = document.createElement('script');
    script.src = 'https://cdn.jsdelivr.net/npm/jsqr@1.4.0/dist/jsQR.js';
    script.async = true;
    document.head.appendChild(script);
}