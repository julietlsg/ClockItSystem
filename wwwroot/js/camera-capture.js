const video = document.getElementById("video");
const canvas = document.getElementById("canvas");
const resultBox = document.getElementById("resultBox");

let isProcessing = false;
let cameraStarted = false;
let faceCurrentlyInFrame = false;
let faceMonitorInterval = null;

async function loadFaceApiModels() {
    try {
        showStatus("Loading face detection model...", "info");

        if (typeof faceapi === "undefined") {
            showStatus("Face API library did not load. Check internet connection.", "error");
            throw new Error("faceapi is undefined");
        }

        await faceapi.nets.tinyFaceDetector.loadFromUri(
            "https://justadudewhohacks.github.io/face-api.js/models"
        );

        showStatus("Face detection ready. Starting camera...", "info");
    }
    catch (error) {
        console.error("Face API model loading error:", error);
        showStatus("Unable to initialise facial verification.", "error");
        throw error;
    }
}
async function startCamera() {
    try {
        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            showStatus("Camera API is not supported by this browser.", "error");
            return;
        }

        const stream = await navigator.mediaDevices.getUserMedia({
            video: {
                facingMode: "user",
                width: { ideal: 640 },
                height: { ideal: 480 }
            },
            audio: false
        });

        video.srcObject = stream;

        video.onloadedmetadata = async () => {
            await video.play();

            cameraStarted = true;

            showStatus("Camera ready. Place face within the scan area.", "info");

            startFaceMonitoring();
        };
    }
    catch (error) {
        console.error("Camera access error:", error);

        if (error.name === "NotAllowedError") {
            showStatus("Camera permission denied. Please allow camera access.", "error");
        } else if (error.name === "NotFoundError") {
            showStatus("No camera was found on this device.", "error");
        } else if (error.name === "NotReadableError") {
            showStatus("Camera is already in use by another application.", "error");
        } else {
            showStatus("Unable to start camera. Please check browser permissions.", "error");
        }
    }
}

function startFaceMonitoring() {
    if (faceMonitorInterval) {
        clearInterval(faceMonitorInterval);
    }

    faceMonitorInterval = setInterval(async () => {
        if (!cameraStarted || isProcessing) {
            return;
        }

        await checkForFace();

    }, 800);
}

async function checkForFace() {
    try {
        const detection = await faceapi.detectSingleFace(
            video,
            new faceapi.TinyFaceDetectorOptions({
                inputSize: 224,
                scoreThreshold: 0.5
            })
        );

        if (detection) {
            if (!faceCurrentlyInFrame) {
                faceCurrentlyInFrame = true;
                await captureAndVerify();
            } else {
                showStatus("Face detected. Please wait for next student.", "info");
            }
        } else {
            faceCurrentlyInFrame = false;
            showStatus("Waiting for face. Please place face within the scan area.", "info");
        }
    }
    catch (error) {
        console.error("Face detection error:", error);
        showStatus("Face detection failed.", "error");
    }
}

async function captureAndVerify() {
    try {
        isProcessing = true;

        showStatus("Face detected. Verifying attendance...", "info");

        const context = canvas.getContext("2d");

        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;

        if (canvas.width === 0 || canvas.height === 0) {
            showStatus("Camera not ready yet. Please wait.", "error");
            return;
        }

        context.drawImage(video, 0, 0, canvas.width, canvas.height);

        const imageBase64 = canvas.toDataURL("image/png");

        const response = await fetch("/Biometric/VerifyFace", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                imageBase64: imageBase64
            })
        });

        const result = await response.json();

        if (result.success) {
            showStatus(
                `Attendance recorded successfully. Match Score: ${result.score}%`,
                "success"
            );
        } else {
            showStatus(result.message, "error");
        }
    }
    catch (error) {
        console.error("Verification error:", error);
        showStatus("Verification failed. Please try again.", "error");
    }
    finally {
        isProcessing = false;
    }
}

function showStatus(message, type) {
    resultBox.classList.remove("success", "error", "info");
    resultBox.classList.add(type);
    resultBox.innerText = message;
}

window.addEventListener("load", async () => {
    try {
        await loadFaceApiModels();
        await startCamera();
    }
    catch {
        showStatus("Unable to initialise facial verification.", "error");
    }
});