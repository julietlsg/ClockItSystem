const video = document.getElementById("video");
const canvas = document.getElementById("canvas");
const resultBox = document.getElementById("resultBox");

let isProcessing = false;
let cameraStarted = false;
let faceCurrentlyInFrame = false;
let faceMonitorInterval = null;
let messageLocked = false;

async function loadFaceApiModels() {
    try {
        showTextStatus("Loading face detection model...", "info");

        if (typeof faceapi === "undefined") {
            throw new Error("faceapi is undefined");
        }

        await faceapi.nets.tinyFaceDetector.loadFromUri(
            "https://justadudewhohacks.github.io/face-api.js/models"
        );

        await faceapi.nets.faceLandmark68Net.loadFromUri(
            "https://justadudewhohacks.github.io/face-api.js/models"
        );

        await faceapi.nets.faceRecognitionNet.loadFromUri(
            "https://justadudewhohacks.github.io/face-api.js/models"
        );

        showTextStatus("Face detection ready. Starting camera...", "info");
    }
    catch (error) {
        console.error("Face API model loading error:", error);
        showTextStatus("Unable to initialise facial verification.", "error");
        throw error;
    }
}

async function startCamera() {
    try {
        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            showTextStatus("Camera API is not supported by this browser.", "error");
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

            showTextStatus("Camera ready. Place face within the scan area.", "info");

            startFaceMonitoring();
        };
    }
    catch (error) {
        console.error("Camera access error:", error);

        if (error.name === "NotAllowedError") {
            showTextStatus("Camera permission denied. Please allow camera access.", "error");
        } else if (error.name === "NotFoundError") {
            showTextStatus("No camera was found on this device.", "error");
        } else if (error.name === "NotReadableError") {
            showTextStatus("Camera is already in use by another application.", "error");
        } else {
            showTextStatus("Unable to start camera. Please check browser permissions.", "error");
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
            if (!faceCurrentlyInFrame && !messageLocked) {
                faceCurrentlyInFrame = true;
                await captureAndVerify();
            }
        } else {
            faceCurrentlyInFrame = false;

            if (!messageLocked) {
                showTextStatus("Waiting for face. Please place face within the scan area.", "info");
            }
        }
    }
    catch (error) {
        console.error("Face detection error:", error);

        if (!messageLocked) {
            showTextStatus("Face detection failed.", "error");
        }
    }
}

async function captureAndVerify() {
    try {
        isProcessing = true;

        showTextStatus("Face detected. Verifying attendance...", "info");

        const detection = await faceapi
            .detectSingleFace(
                video,
                new faceapi.TinyFaceDetectorOptions({
                    inputSize: 224,
                    scoreThreshold: 0.5
                })
            )
            .withFaceLandmarks()
            .withFaceDescriptor();

        if (!detection) {
            showTextStatus("No clear face detected. Please look directly at the camera.", "error");
            return;
        }

        const descriptorArray = Array.from(detection.descriptor);
        const descriptorJson = JSON.stringify(descriptorArray);

        const context = canvas.getContext("2d");

        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;

        if (canvas.width === 0 || canvas.height === 0) {
            showTextStatus("Camera not ready yet. Please wait.", "error");
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
                imageBase64: imageBase64,
                descriptorJson: descriptorJson
            })
        });

        const result = await response.json();

        if (result.success) {
            messageLocked = true;

            showSuccessStatus(
                result.message,
                result.studentName,
                result.studentNumber
            );

            setTimeout(() => {
                messageLocked = false;

                showTextStatus(
                    "Please allow the next student to place their face within the scan area.",
                    "info"
                );
            }, 4000);
        } else {
            messageLocked = true;

            showTextStatus(result.message, "error");

            setTimeout(() => {
                messageLocked = false;

                showTextStatus(
                    "Waiting for face. Please place face within the scan area.",
                    "info"
                );
            }, 5000);
        }
    }
    catch (error) {
        console.error("Verification error:", error);
        showTextStatus("Verification failed. Please try again.", "error");
    }
    finally {
        isProcessing = false;
    }
}

function showTextStatus(message, type) {
    resultBox.classList.remove("success", "error", "info");
    resultBox.classList.add(type);
    resultBox.innerText = message;
}

function showSuccessStatus(title, studentName, studentNumber) {
    resultBox.classList.remove("success", "error", "info");
    resultBox.classList.add("success");

    resultBox.innerHTML = `
        <div class="attendance-success">
            <div class="success-check">✓</div>
            <div class="success-title">${title}</div>
            <div class="success-student">${studentName}</div>
            <div class="success-number">Student No: ${studentNumber}</div>
        </div>
    `;
}

window.addEventListener("load", async () => {
    try {
        await loadFaceApiModels();
        await startCamera();
    }
    catch {
        showTextStatus("Unable to initialise facial verification.", "error");
    }
});