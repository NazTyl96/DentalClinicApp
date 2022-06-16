document.querySelector('.section-list__section#home').onclick = function () {
    document.location.assign('/Home/Index')
}

document.querySelector('.section-list__section#patient').onclick = function () {
    document.location.assign('/Patient/Index')
}

document.querySelector('.section-list__section#document').onclick = function () {
    document.location.assign('/Document/Index')
}

document.querySelector('.section-list__section#calendar').onclick = function () {
    document.location.assign('/Appointment/Index')
}

document.querySelector('.section-list__section#doctor').onclick = function () {
    document.location.assign('/Doctor/Index')
}

document.querySelector('.section-list__section#service').onclick = function () {
    document.location.assign('/Service/Index')
}

const checkForNewNotifications = () => {
    notificationModal = new bootstrap.Modal(document.getElementById('view-notification-modal'), {
        keyboard: false
    });

    $.ajax({
        url: `/Home/CheckForNewNotifications`,
        method: 'GET',
        beforeSend: function () {
            $("#partial-notification-insert-view").empty().html("<h4>Загрузка данных...</h4>");
        },
        success: function (result) {
            if (result.length > 0) {
                $("#partial-notification-insert-view").html(result);

                notificationModal.show();
            }
        }
    });
}