document.querySelector('.section-list__section#calendar').classList.add('section-list__section_active');

let addEditModal;
let viewModal;
let deleteConfirmModal;

document.addEventListener('DOMContentLoaded', () => {
    checkForNewNotifications();

    $('#patient-filter').select2({
        theme: "bootstrap-5",
        placeholder: 'Select patient'
    });

    $('#doctor-filter').select2({
        theme: "bootstrap-5",
        placeholder: 'Select doctor'
    });

    $('#service-filter').select2({
        theme: "bootstrap-5",
        placeholder: 'Select service'
    });

    loadCalendar();
});

const clearFilters = () => {

    $('#patient-filter').val('');
    $('#patient-filter').trigger('change');

    $('#doctor-filter').val('');
    $('#doctor-filter').trigger('change');

    $('#service-filter').val('');
    $('#service-filter').trigger('change');

    loadCalendar();
}

//switch archived/active appointments
const changeStatus = () => {

    const curStatus = document.location.search.split('=')[1] || '1';

    let newStatus;
    if (curStatus === '1') {
        newStatus = '0';
    }
    else {
        newStatus = '1';
    }

    document.location.assign(`/Appointment/Index?status=${newStatus}`)
}

//declared as a funtion to be accessible from another browser tab
function addAppointment(date, patientId = 0, doctorId = 0) {
    addEditModal = new bootstrap.Modal(document.getElementById('add-edit-modal'), {
        keyboard: false
    });

    const container = document.getElementById('partial-insert');

    document.getElementById('add-app-modal-label').textContent = 'Adding an appointment';
    container.innerHTML = '<h4>Loading...</h4>'

    fetch(`/Appointment/AddAppointment?${new URLSearchParams({
        date: JSON.stringify(date),
        patientId: patientId,
        doctorId: doctorId
    })}`)
    .then(response => response.text())
    .then(html => container.innerHTML = html)
    .then(() => {
        document.getElementById('add-edit-modal').addEventListener('shown.bs.modal', function () {
            document.getElementById('patient-select').focus()
        })

        $('#patient-select').select2({
            theme: "bootstrap-5",
            placeholder: 'Select patient',
            dropdownParent: $('#add-edit-modal')
        });

        $('#doctor-select').select2({
            theme: "bootstrap-5",
            placeholder: 'Select doctor',
            dropdownParent: $('#add-edit-modal')
        });

        $('#service-select').select2({
            theme: "bootstrap-5",
            placeholder: 'Select service',
            dropdownParent: $('#add-edit-modal')
        });

        addEditModal.show();
    })
    .catch(error => console.error(error));
};

const editAppointment = (e) => {
    const appId = e.currentTarget.closest('tr').dataset.appointmentId;
    addEditModal = new bootstrap.Modal(document.getElementById('add-edit-modal'), {
        keyboard: false
    });

    const container = document.getElementById('partial-insert');
    container.innerHTML = '<h4>Loading...</h4>'
    document.getElementById('add-app-modal-label').textContent = 'Editing the appointment information';

    fetch(`/Appointment/EditAppointment?id=${appId}`)
    .then(response => response.text())
    .then(html => container.innerHTML = html)
    .then(() => {
        $('#patient-select').select2({
            theme: "bootstrap-5",
            placeholder: 'Select patient',
            dropdownParent: $('#add-edit-modal')
        });

        $('#doctor-select').select2({
            theme: "bootstrap-5",
            placeholder: 'Select doctor',
            dropdownParent: $('#add-edit-modal')
        });

        $('#service-select').select2({
            theme: "bootstrap-5",
            placeholder: 'Select service',
            dropdownParent: $('#add-edit-modal')
        });

        addEditModal.show();
    })
    .catch(error => console.error(error));
};

const saveAppointment = () => {
    /*const appForm = $('#app-form').serialize();*/
    const date = document.getElementById('date-input').value;

    const appForm = document.getElementById('app-form');
    const appFormData = new FormData(appForm);
    const appFormValue = Object.fromEntries(appFormData.entries());


    fetch(`/Appointment/SaveAppointment`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({ ...appFormValue, Date: date })
    })
    .then(response => response.json())
    .then(result => {
        if (result.success) {
            const parentWin = window.opener;

            if (parentWin) {
                if (typeof parentWin.toggleViewModal == "function") {
                    parentWin.toggleViewModal();
                }
                if (typeof parentWin.viewPatient == "function") {
                    parentWin.viewPatient(document.getElementById('patient-select').value);
                }
                if (typeof parentWin.ViewDoctor == "function") {
                    parentWin.viewDoctor(document.getElementById('doctor-select').value);
                }
                parentWin.ohSnap("The appointment has been succesfully saved", { 'color': 'green', 'duration': '3000' });
            }
            addEditModal.toggle();
            loadCalendar();
            if (viewModal) {
                viewModal.dispose();
            }
            viewAppointmentsByDate(date.split('T')[0]);
            ohSnap("The appointment has been succesfully saved", { 'color': 'green', 'duration': '3000' });
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

const deleteAppointment = () => {
    const id = document.getElementById('confirm-modal-appointment-id').value;
    const date = document.getElementById('app-date').value;

    fetch(`/Appointment/DeleteAppointment`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({ id: id })
    })
    .then(response => response.json())
    .then(result => {
        if (result.success) {
            ohSnap("The appointment has been canceled", { 'color': 'green', 'duration': '3000' });
            loadCalendar();
            viewAppointmentsByDate(date);
        }
    })
    .catch(error => console.error(error));
}

const appointmentViewScripts = (date) => {
    const dateParts = date.split('-');
    document.getElementById('view-modal-label').textContent = `Appointments on 
                                                               ${dateParts[2]}.${dateParts[1]}.${dateParts[0]}`;
    viewModal.show();

    document.getElementById('add-appointment').onclick = function () {
        addAppointment(date);
    }

    const deleteButtons = Array.from(document.getElementsByClassName('delete-app'));
    deleteButtons.forEach((btn) => {
        btn.onclick = () => {
            deleteConfirmModal = new bootstrap.Modal(document.getElementById('delete-confirm-modal'), {
                keyboard: false
            });

            const appId = btn.closest('tr').dataset.appointmentId;
            const deleteAppBtn = document.getElementById('confirm-delete-app');
            document.getElementById('confirm-modal-appointment-id').value = appId;

            deleteConfirmModal.show();
        }
    })
}

const viewAppointmentsByDate = (date) => {
    viewModal = new bootstrap.Modal(document.getElementById('view-modal'), {
        keyboard: false
    });

    const container = document.getElementById('partial-insert-view');
    const searchParams = {
        PatientsNames: $('#patient-filter').val() || '',
        DoctorIds: $('#doctor-filter').val() || '',
        ServiceIds: $('#service-filter').val() || '',
        Date: date
    };

    container.innerHTML = '<h4>Loading...<h4>';

    fetch(`/Appointment/ViewAppointmentsByDate?queryString=${JSON.stringify(searchParams)}`)
    .then(response => response.text())
    .then(html => container.innerHTML = html)
    .then(() => appointmentViewScripts(date))
    .catch(error => console.error(error));
};

const loadCalendar = () => {
    const container = document.getElementById('calendar-div');
    const searchParams = {
        PatientsNames: $('#patient-filter').val() || '',
        DoctorIds: $('#doctor-filter').val() || '',
        ServiceIds: $('#service-filter').val() || '',
        FirstOfMonth: document.getElementById('month-filter').value + '-01',
        Status: document.location.search.split('=')[1] || '1'
    };

    container.innerHTML = '<h4>Loading...<h4>';

    fetch(`/Appointment/LoadCalendar?queryString=${JSON.stringify(searchParams)}`)
    .then(response => response.text())
    .then(html => container.innerHTML = html)
    .then(() => {
        const calendarTds = Array.from(document.querySelectorAll('.calendar-cell'));
        calendarTds.forEach((td) => {
            td.onclick = () => {
                viewAppointmentsByDate(td.dataset.date);
            }
        })
    })
    .catch(error => console.error(error));
};