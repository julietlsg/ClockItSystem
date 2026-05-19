const video = document.getElementById("video");
const canvas = document.getElementById("canvas");
const resultBox = document.getElementById("resultBox");
const studentIdInput = document.getElementById("studentId");

let isProcessing = false;
let cameraStarted = false;

async function loadFaceApiModels() {
    try {
        showStatus("Loading face enrolment model...", "info");

        if (typeof faceapi === "undefined") {
            throw new Error("faceapi is undefined");
        }

        await faceapi.nets.tinyFaceDetector.loadFromUri("https://justadudewhohacks.github.io/face-api.js/models");
        await faceapi.nets.faceLandmark68Net.loadFromUri("https://justadudewhohacks.github.io/face-api.js/models");
        await faceapi.nets.faceRecognitionNet.loadFromUri("https://justadudewhohacks.github.io/face-api.js/models");

        showStatus("Models ready. Starting camera...", "info");
    }
    catch (error) {
        console.error("Model loading error:", error);
        showStatus("Unable to load face enrolment models.", "error");
        throw error;
    }
}

async function startCamera() {
    try {
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
            showStatus("Camera ready. Keep face steady inside the circle.", "info");

            startEnrollmentWatcher();
        };
    }
    catch (error) {
        console.error("Camera error:", error);
        showStatus("Unable to start camera. Please check permissions.", "error");
    }
}

function startEnrollmentWatcher() {
    setInterval(async () => {
        if (!cameraStarted || isProcessing) {
            return;
        }

        await enrollFace();

    }, 2000);
}

async function enrollFace() {
    try {
        isProcessing = true;

        const detection = await faceapi
            .detectSingleFace(video, new faceapi.TinyFaceDetectorOptions({
                inputSize: 224,
                scoreThreshold: 0.5
            }))
            .withFaceLandmarks()
            .withFaceDescriptor();

        if (!detection) {
            showStatus("No face detected. Place face inside the circle.", "info");
            return;
        }

        showStatus("Face detected. Enrolling...", "info");

        const descriptorArray = Array.from(detection.descriptor);
        const descriptorJson = JSON.stringify(descriptorArray);

        const context = canvas.getContext("2d");

        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;

        context.drawImage(video, 0, 0, canvas.width, canvas.height);

        const imageBase64 = canvas.toDataURL("image/png");

        const response = await fetch("/Biometric/EnrollFace", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                studentId: parseInt(studentIdInput.value),
                imageBase64: imageBase64,
                descriptorJson: descriptorJson
            })
        });

        const result = await response.json();

        if (result.success) {
            showStatus(result.message, "success");
            stopCamera();
        } else {
            showStatus(result.message, "error");
        }
    }
    catch (error) {
        console.error("Enrollment error:", error);
        showStatus("Face enrolment failed.", "error");
    }
    finally {
        isProcessing = false;
    }
}

function stopCamera() {
    if (video.srcObject) {
        const tracks = video.srcObject.getTracks();

        tracks.forEach(track => track.stop());

        cameraStarted = false;
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
        showStatus("Unable to initialise face enrolment.", "error");
    }
});