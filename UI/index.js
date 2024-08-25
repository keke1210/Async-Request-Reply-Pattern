const submitBtn = document.getElementById('submitBtn');
const statusDiv = document.getElementById('status');
const resultDiv = document.getElementById('result');

let isProcessing = false;

submitBtn.addEventListener('click', () => {
    if (!isProcessing) {
        submitRequest();
    } else {
        statusDiv.textContent = 'A request is already in progress. Please wait.';
    }
});

async function submitRequest() {
    isProcessing = true;
    statusDiv.textContent = 'Submitting request...';
    resultDiv.textContent = '';

    try {
        const response = await fetch("http://localhost:7279/api/SubmitRequest", {
            method: 'POST'
        });

        if (response.ok) {
            const location = response.headers.get('Location');
            const retryAfter = parseInt(response.headers.get('Retry-After'), 10) * 1000;

            statusDiv.textContent = 'Request accepted. Checking status...';
            setTimeout(() => checkResult(location), retryAfter);
        } else {
            throw new Error('Failed to submit request');
        }
    } catch (error) {
        statusDiv.textContent = `Error: ${error.message}`;
        isProcessing = false;
    }
}

async function checkResult(location) {
    try {
        const response = await fetch(location, { redirect: 'follow' });

        if (response.ok) {
            if (response.url.includes('GetResult')) {
                // We've been redirected to the GetResult endpoint
                const result = await response.text();
                statusDiv.textContent = 'Processing completed.';
                resultDiv.textContent = `Result: ${result}`;
                isProcessing = false;
            } else {
                // Still processing
                statusDiv.textContent = 'Still processing. Checking again...';
                setTimeout(() => checkResult(location), 2000);
            }
        } else {
            throw new Error(`Unexpected response: ${response.status} ${response.statusText}`);
        }
    } catch (error) {
        statusDiv.textContent = `Error: ${error.message}`;
        isProcessing = false;
    }
}

// The getResult function is not used in this flow, so I've removed it.