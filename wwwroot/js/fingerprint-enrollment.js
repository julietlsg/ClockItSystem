const statusBox =
    document.getElementById(
        "scannerStatus");

document
    .getElementById(
        "btnEnrollFingerprint")
    .addEventListener("click", () => {

        statusBox.innerHTML =
            "Fingerprint enrollment coming next.";

    });