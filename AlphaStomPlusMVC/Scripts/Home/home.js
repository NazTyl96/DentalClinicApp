document.querySelector('.section-list__section#home').classList.add('section-list__section_active');

document.addEventListener('DOMContentLoaded', () => {
    checkForNewNotifications();
});

const viewNotifications = () => {
    notificationModal = new bootstrap.Modal(document.getElementById('view-notification-modal'), {
        keyboard: false
    });

    const container = document.getElementById('partial-notification-insert-view');

    container.innerHTML = '<h4>Loading...</h4>'

    fetch(`/Home/GetAllNotifications`)
    .then(response => response.text())
    .then(html => container.innerHTML = html)
    .then(() => {
        notificationModal.show();
    })
    .catch(error => console.error(error));
}

const addPatient = () => {
    const patientWin = window.open(`/Patient/Index`, '_blank');
    patientWin.onload = () => {
        patientWin.addPatient();
    };
}

const addDocument =  () => {
    const docWin = window.open(`/Document/Index`, '_blank');
    docWin.onload = function () {
        docWin.addDocument();
    };
}

const addAppointment = () => {
    const appWin = window.open(`/Appointment/Index`, '_blank');
    appWin.onload = function () {
        appWin.addAppointment('');
    };
}

const addDoctor = () => {
    const doctorWin = window.open(`/Doctor/Index`, '_blank');
    doctorWin.onload = function () {
        doctorWin.AddDoctor();
    };
}

const addService = () => {
    const serviceWin = window.open(`/Service/Index`, '_blank');
    serviceWin.onload = function () {
        serviceWin.AddService();
    };
}

const getNotificationsCount = () => {

    const container = document.getElementById('notifications-count');

    fetch(`/Home/GetNotificationsCount`)
    .then(response => response.text())
    .then(data => container.textContent = data)
    .then(data => {
        if (data > 0) {
            container.textContent = data
            container.style.display = 'block';
        }
        else {
            container.style.display = 'none';
        };
    })
    .catch(error => console.error(error));
}

const acceptNotification = (e) => {
    const curTr = e.currentTarget.closest('tr');
    const notificationId = curTr.dataset.notificationId;

    fetch(`/Home/AcceptNotification`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({
            id: notificationId
        })
    })
    .then(response => response.json())
    .then(result => {
        if (result.success) {
            curTr.style.display = 'none';
            ohSnap("This notification will not be shown again", { 'color': 'green', 'duration': '4000' });

            getNotificationsCount();
        }
    })
    .catch(error => console.error(error));
}