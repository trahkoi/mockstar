(function () {
  const submitButton = document.getElementById("ocr-submit");
  const antiForgeryToken = document.querySelector("#ocr-form input[name='__RequestVerificationToken']")?.value;
  if (!submitButton) {
    return;
  }

  submitButton.addEventListener("click", async function () {
    const fileInput = document.getElementById("ocr-file");
    const textField = document.getElementById("ocr-text");
    const status = document.getElementById("ocr-status");
    const form = document.getElementById("ocr-form");
    const file = fileInput?.files?.[0];

    if (!file || !textField || !status || !form) {
      status.textContent = "Choose an image before running OCR.";
      return;
    }

    status.textContent = "Running OCR in the browser...";
    submitButton.setAttribute("disabled", "disabled");

    try {
      const result = await Tesseract.recognize(file, "eng", {
        workerPath: "/vendor/tesseract/worker.min.js"
      });

      textField.value = result.data.text || "";
      status.textContent = "OCR complete. Sending extracted text for review...";

      await htmx.ajax("POST", "/Import?handler=Text", {
        source: form,
        target: "#import-result",
        swap: "innerHTML",
        headers: antiForgeryToken ? { RequestVerificationToken: antiForgeryToken } : {},
        values: {
          extractedText: textField.value
        }
      });
      status.textContent = "Review updated.";
    } catch (error) {
      status.textContent = error instanceof Error ? error.message : "OCR failed.";
    } finally {
      submitButton.removeAttribute("disabled");
    }
  });
})();
