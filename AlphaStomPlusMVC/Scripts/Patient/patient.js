document.querySelector('.section-list__section#patient').classList.add('section-list__section_active');

let addEditModal;
let viewModal;
let deleteConfirmModal;

document.addEventListener('DOMContentLoaded', () => {
    checkForNewNotifications();

    $('#fullname-filter').select2({
        theme: "bootstrap-5",
        placeholder: 'Select name'
    });

    $('#birthdate-filter').select2({
        theme: "bootstrap-5",
        placeholder: 'Select date of birth'
    });

    $('#sex-filter').select2({
        theme: "bootstrap-5",
        placeholder: 'Select sex'
    });

    loadPatientList();
});

const clearFilters = () => {

    $('#fullname-filter').val('');
    $('#fullname-filter').trigger('change');

    $('#birthdate-filter').val('');
    $('#birthdate-filter').trigger('change');

    $('#sex-filter').val('');
    $('#sex-filter').trigger('change');

    loadPatientList();
}

//function to call from a child window(browser tab)
function toggleViewModal() {
    viewModal.toggle();
};

//switch archived/active patients
const changeStatus = () => {

    const curStatus = document.location.search.split('=')[1] || '1';

    let newStatus;
    if (curStatus === '1') {
        newStatus = '0';
    }
    else {
        newStatus = '1';
    }

    document.location.assign(`/Patient/Index?status=${newStatus}`)
}

//scripts to be loaded when in add form
const addScripts = () => {
    const modal = document.getElementById('add-edit-modal');
    const fullNameInput = document.getElementById('full-name');

    modal.addEventListener('shown.bs.modal', () => {
        fullNameInput.focus()
    })

    const checkboxes = Array.from(document.getElementsByClassName('form-check-inline'));
    checkboxes.forEach((chk) => {
        chk.onchange = () => {
            if (chk.checked) {
                chk.value = true;
            }
            else {
                chk.value = false;
            }
        }
    });

    $('#doctor-select').select2({
        theme: "bootstrap-5",
        placeholder: 'Choose a doctor',
        dropdownParent: $('#add-edit-modal')
    });

    $('#service-select').select2({
        theme: "bootstrap-5",
        placeholder: 'Choose a service',
        dropdownParent: $('#add-edit-modal')
    });
}

//declared as a function to be accessible from another browser tab
function addPatient() {
    addEditModal = new bootstrap.Modal(document.getElementById('add-edit-modal'), {
        keyboard: false
    });

    document.getElementById('save-patient').style.display = 'block';
    document.getElementById('add-patient-modal-label').textContent = 'Adding information about a patient';

    const container = document.getElementById('partial-insert');

    container.innerHTML = '';

    fetch(`/Patient/AddPatient`)
    .then(response => response.text())
    .then(html => container.innerHTML = html)
    .then(() => {
        addScripts();
        addEditModal.show();
    })
    .catch(error => console.error(error));
};

//scripts to be loaded when in view form
const viewScripts = () => {
    const patientId = document.getElementById('patient-id').value;

    const addDocButton = document.getElementById('add-document');
    addDocButton.onclick = () => {
        const docWin = window.open('/Document/Index', '_blank');
        docWin.onload = () => {
            docWin.addDocument(patientId);
        };

        docWin.onbeforeunload = () => {
            viewModal.toggle();
            viewPatient(patientId);
            ohSnap("Document is successfully saved", { 'color': 'green', 'duration': '3000' });
        }
    }

    const addAppButton = document.getElementById('add-appointment');
    addAppButton.onclick = () => {
        const appWin = window.open('/Appointment/Index', '_blank');
        appWin.onload = () => {
            appWin.addAppointment('', patientId);
        };
    }

    const viewDocument = (docId) => {
        fetch(`/Document/ViewDocument?docId=${docId}`)
        .then(response => response.blob())
        .then(blob => {
            window.open(window.URL.createObjectURL(blob));
        })
        .catch(error => console.error(error));
    }

    const documentTds = document.querySelectorAll('.doc-tr td:nth-child(-n + 3)');
    Array.from(documentTds).forEach((td) => {
        td.onclick = () => {
            const docId = td.closest("tr").dataset.docId;
            viewDocument(docId);
        }
    })

    document.getElementById('print-patient-card').onclick = () => {
        window.open(`/Patient/PatientCard?id=${patientId}`);
    }

    document.getElementById('print-contract').onclick = () => {
        window.open(`/Patient/PatientDogovorWord?id=${patientId}`);
    }

    document.getElementById('print-consent').onclick = () => {
        window.open(`/Patient/PatientConsentWord?id=${patientId}`);
    }
}

//declared as a function to be accessible from another browser tab
function viewPatient(patientId) {
    viewModal = new bootstrap.Modal(document.getElementById('view-modal'), {
        keyboard: false
    });

    const container = document.getElementById('partial-insert-view')

    container.innerHTML = '';

    fetch(`/Patient/ViewPatient?id=${patientId}`)
    .then(response => response.text())
    .then(html => container.innerHTML = html)
    .then(() => {
        viewScripts();
        viewModal.show();
    })
    .catch(error => console.error(error));
};

//scripts to be loaded after the list of patients is displayed
const afterLoadScripts = () => {

    $("#patients-list-table").tablesorter({
        cssIcon: 'tablesorter-icon',
        headerTemplate: '{icon}{content}',
        cssChildRow: "tablesorter-childRow"
    });

    const patientTds = Array.from(document.querySelectorAll('table tbody tr td:nth-child(-n + 5)'));
    patientTds.forEach((td) => {
        td.onclick = () => {
            viewPatient(td.closest('tr').dataset.patientId);
        }
    })

    const editButtons = Array.from(document.getElementsByClassName('edit-patient'));
    editButtons.forEach((btn) => {
        btn.onclick = () => {
            editPatient(btn.closest('tr').dataset.patientId);
        }
    })

    const deleteRestoreButtons = Array.from(document.getElementsByClassName('delete-restore-btn'));
    deleteRestoreButtons.forEach((btn) => {
        btn.onclick = () => {
            deleteConfirmModal = new bootstrap.Modal(document.getElementById('delete-confirm-modal'), {
                keyboard: false
            });

            const patientId = btn.closest('tr').dataset.patientId;
            document.getElementById('confirm-modal-patient-id').value = patientId;

            const question = document.getElementById('delete-confirm-modal__question');

            if (btn.classList.contains('delete-patient')) {
                question.innerHTML = `<span id="delete-confirm-modal__warning">Moving patient to the archive will result in all his upcoming appointments canceled and documents deleted from the system</span>
                                         <br />
                                         <span id="delete-confirm-modal__question">Are you sure you want to move this patient to the archive?</span>`;
                const deletePatientBtn = document.getElementById('delete-patient');
                document.getElementById('restore-patient').style.display = 'none';
                deletePatientBtn.style.display = 'block';
            }
            else {
                question.textContent = 'Are you sure you want to move this patient back to the active patients?';
                const restorePatientBtn = document.getElementById('restore-patient');
                document.getElementById('delete-patient').style.display = 'none';
                restorePatientBtn.style.display = 'block';
            }

            deleteConfirmModal.show();
        }
    })
}

const loadPatientList = () => {
    const container = document.getElementById('table-div');
    const searchParams = {
        FullNames: $('#fullname-filter').val() || '',
        DatesOfBirth: $('#birthdate-filter').val()|| '',
        Sexes: $('#sex-filter').val() || '',
        Status: document.location.search.split('=')[1] || '1'
    };

    container.innerHTML = '<h4>Loading...<h4>';

    fetch(`/Patient/LoadPatientsList?queryString=${JSON.stringify(searchParams)}`)
    .then(response => response.text())
    .then(html => container.innerHTML = html)
    .then(() => afterLoadScripts())
    .catch(error => console.error(error));
};

const editPatient = (patientId) => {
    addEditModal = new bootstrap.Modal(document.getElementById('add-edit-modal'), {
        keyboard: false
    });

    const container = document.getElementById('partial-insert')

    container.innerHTML = '';
    document.getElementById('save-patient').style.display = 'block';
    document.getElementById('add-patient-modal-label').textContent = 'Editing information about a patient';

    fetch(`/Patient/EditPatient?id=${patientId}`)
    .then(response => response.text())
    .then(html => container.innerHTML = html)
    .then(() => {
        const checkboxes = Array.from(document.getElementsByClassName('form-check-inline'));
        checkboxes.forEach((chk) => {
            chk.onchange = () => {
                if (chk.checked) {
                    chk.value = true;
                }
                else {
                    chk.value = false;
                }
            }
        })

        addEditModal.show();
    })
    .catch(error => console.error(error));
};

const savePatient = () => {
    const patientForm = document.getElementById('patient-form');
    const patientFormData = new FormData(patientForm);
    const patientFormValue = Object.fromEntries(patientFormData.entries());

    let body;
    const appointmentForm = document.getElementById('appointment-form');
    if (appointmentForm) {
        const appointmentFormData = new FormData(appointmentForm);
        const appointmentFormValue = Object.fromEntries(appointmentFormData.entries());
        body = {
            patient: patientFormValue,
            newAppointment: appointmentFormValue
        }
    }
    else {
        body = {
            patient: patientFormValue,
        }
    }

    fetch(`/Patient/SavePatient`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(body)
    })
    .then(response => response.json())
    .then(result => {
        if (result.success) {
            addEditModal.toggle();
            loadPatientList();
            ohSnap("All changes are successfully saved", { 'color': 'green', 'duration': '4000' });
            if (result.data > 0) {
                viewPatient(result.data);
            }
        }
        else {
            const validBlock = document.getElementById('add-edit-validation');
            validBlock.querySelector('ul').innerHTML = '';
            (result.data).forEach(function (item) {
                validBlock.querySelector('ul').innerHTML += '<li>' + item + '</li>';
            })

            $('#add-edit-modal').animate({ scrollTop: 0 }, 'slow');
        }
    })
    .catch(error => console.error(error));
};

const deletePatient = () => {
    const patientId = document.getElementById('confirm-modal-patient-id').value;

    fetch(`/Patient/DeletePatient`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({
            patientId: patientId
        })
    })
    .then(response => response.json())
    .then(result => {
        if (result.success) {
            deleteConfirmModal.toggle();
            loadPatientList();
            ohSnap("Patient has been moved to archive", { 'color': 'green', 'duration': '3000' });
        }
    })
    .catch(error => console.error(error));
}

const restorePatient = () => {
    const patientId = document.getElementById('confirm-modal-patient-id').value;

    fetch(`/Patient/RestorePatient`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({
            patientId: patientId
        })
    })
    .then(response => response.json())
    .then(result => {
        if (result.success) {
            deleteConfirmModal.toggle();
            loadPatientList();
            ohSnap("Patient has been moved back to active", { 'color': 'green', 'duration': '3000' });
        }
    })
    .catch(error => console.error(error));
}