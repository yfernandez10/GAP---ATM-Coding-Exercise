const operation = document.getElementById("Form_ActionType");
const source = document.getElementById("Form_SelectedAccount");
const destination = document.getElementById("Form_DestinationAccount");
const destinationField = document.getElementById("destination-field");
const sourceLabel = document.getElementById("account-label");

function selectOtherAccount(select, account) {
    select.value = Array.from(select.options).find(option => option.value && option.value !== account).value;
}

function updateForm() {
    const isTransfer = operation.value === operation.dataset.transferValue;
    sourceLabel.textContent = isTransfer ? "From account" : "Account";
    destinationField.hidden = !isTransfer;
    destination.disabled = !isTransfer;
    destination.required = isTransfer;
    if (isTransfer && (!destination.value || source.value === destination.value)) {
        selectOtherAccount(destination, source.value);
    }
}

operation.addEventListener("change", updateForm);
source.addEventListener("change", updateForm);
destination.addEventListener("change", () => {
    if (destination.value === source.value) {
        selectOtherAccount(source, destination.value);
    }
});
updateForm();
