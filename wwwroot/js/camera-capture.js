let video = document.getElementById("video");
let canvas = document.getElementById("canvas");
let captureButton = document.getElementById("captureBtn");

async function startCamera() {
    try {
        const stream = await navigator.mediaDevices.getUserMedia({
            video: true
        });

        video.srcObject = stream;
    }
    catch (error) {
        console.error("Error accessing camera:", error);
        alert("Unable to access camera.");
    }
}

captureButton.addEventListener("click", async () => {

    const context = canvas.getContext("2d");

    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;

    context.drawImage(video, 0, 0, canvas.width, canvas.height);

    const imageBase64 = canvas.toDataURL("image/png");

    try {

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

            alert(`Attendance recorded for student ID: ${result.studentId}`);

        } else {

            alert(result.message);
        }

    } catch (error) {

        console.error(error);
        alert("Verification failed.");
    }
});

window.onload = startCamera;