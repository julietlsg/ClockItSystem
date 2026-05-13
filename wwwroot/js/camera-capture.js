const video = document.getElementById("video");
const canvas = document.getElementById("canvas");
const resultBox = document.getElementById("resultBox");

let isProcessing = false;
let hasFacePreviously = false;

async function startCamera() {

    try {

        const stream = await navigator.mediaDevices.getUserMedia({
            video: true,
            audio: false
        });

        video.srcObject = stream;

        video.onloadedmetadata = async () => {

            showResult("Camera ready. Waiting for student...", true);

            await startFaceMonitoring();
        };

    } catch (error) {

        console.error(error);

        showResult("Unable to access camera.", false);
    }
}

async function startFaceMonitoring() {

    setInterval(async () => {

        if (isProcessing)
            return;

        const detections = await faceapi.detectAllFaces(
            video,
            new faceapi.TinyFaceDetectorOptions()
        );

        if (detections.length > 0) {

            if (!hasFacePreviously) {

                hasFacePreviously = true;

                await captureAndVerify();
            }

        } else {

            hasFacePreviously = false;

            showResult("Waiting for student...", true);
        }

    }, 1000);
}

async function captureAndVerify() {

    try {

        isProcessing = true;

        showResult("Face detected. Verifying...", true);

        const context = canvas.getContext("2d");

        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;

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

            showResult(
                `Attendance recorded successfully. Match Score: ${result.score}%`,
                true
            );

        } else {

            showResult(result.message, false);
        }

    } catch (error) {

        console.error(error);

        showResult("Verification failed.", false);

    } finally {

        isProcessing = false;
    }
}

function showResult(message, isSuccess) {

    resultBox.classList.remove(
        "d-none",
        "alert-success",
        "alert-danger"
    );

    resultBox.classList.add(
        isSuccess ? "alert-success" : "alert-danger"
    );

    resultBox.innerText = message;
}


async function loadModels() {

    await faceapi.nets.tinyFaceDetector.loadFromUri(
        "https://justadudewhohacks.github.io/face-api.js/models"
    );
}

window.addEventListener("load", async () => {

    await loadModels();

    await startCamera();
});