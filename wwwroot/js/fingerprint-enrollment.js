document
    .getElementById("captureBtn")
    .addEventListener("click", async () => {

        const studentId =
            document.getElementById("studentId").value;

        const status =
            document.getElementById("statusMessage");

        status.className =
            "alert alert-info";

        status.innerText =
            "Place finger on scanner...";

        const response =
            await fetch(
                "/Biometric/CaptureAndEnrollFingerprint",
                {
                    method: "POST",
                    headers: {
                        "Content-Type":
                            "application/x-www-form-urlencoded"
                    },
                    body:
                        `studentId=${studentId}`
                });

        const result =
            await response.json();

        if (result.success) {

            status.className =
                "alert alert-success";

            status.innerText =
                result.message;
        }
        else {

            status.className =
                "alert alert-danger";

            status.innerText =
                result.message;
        }
    });