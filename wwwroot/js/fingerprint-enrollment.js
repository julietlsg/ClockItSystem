document
    .getElementById("captureBtn")
    .addEventListener("click", async () => {

        const studentId =
            document.getElementById("studentId").value;

        /*
          TEMPORARY
          Replace with scanner later
        */

        const template =
            "TEST_TEMPLATE_" +
            Date.now();

        const response =
            await fetch(
                "/Biometric/EnrollFingerprint",
                {
                    method: "POST",
                    headers: {
                        "Content-Type":
                            "application/x-www-form-urlencoded"
                    },
                    body:
                        `studentId=${studentId}` +
                        `&fingerprintTemplate=${template}`
                });

        const result =
            await response.json();

        const status =
            document.getElementById("statusMessage");

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