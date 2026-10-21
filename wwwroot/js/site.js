// ParkEase - Interactive Client Logic & Utilities

document.addEventListener('DOMContentLoaded', function () {
    // 1. Auto-dismiss alerts after 6 seconds
    setTimeout(function () {
        var alerts = document.querySelectorAll('.alert-dismissible');
        alerts.forEach(function (alert) {
            var bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            bsAlert.close();
        });
    }, 6000);

    // 2. Demo Login Quick-Fill Helpers
    var fillAdminBtn = document.getElementById('fillAdminCredentials');
    if (fillAdminBtn) {
        fillAdminBtn.addEventListener('click', function () {
            var emailInput = document.getElementById('loginEmail');
            var passInput = document.getElementById('loginPassword');
            if (emailInput && passInput) {
                emailInput.value = 'admin@parkease.com';
                passInput.value = 'Admin@123';
            }
        });
    }

    var fillCustomerBtn = document.getElementById('fillCustomerCredentials');
    if (fillCustomerBtn) {
        fillCustomerBtn.addEventListener('click', function () {
            var emailInput = document.getElementById('loginEmail');
            var passInput = document.getElementById('loginPassword');
            if (emailInput && passInput) {
                emailInput.value = 'john@example.com';
                passInput.value = 'User@123';
            }
        });
    }

    // 3. Live Booking Fee Calculator on Bookings/Create
    var startInput = document.getElementById('bookingStartTime');
    var endInput = document.getElementById('bookingEndTime');
    var rateInput = document.getElementById('slotHourlyRate');

    if (startInput && endInput && rateInput) {
        function recalculateFee() {
            var startVal = new Date(startInput.value);
            var endVal = new Date(endInput.value);
            var hourlyRate = parseFloat(rateInput.value) || 0;

            var billableHoursEl = document.getElementById('displayBillableHours');
            var estimatedFeeEl = document.getElementById('displayEstimatedFee');
            var durationTextEl = document.getElementById('displayDurationText');

            if (isNaN(startVal.getTime()) || isNaN(endVal.getTime()) || endVal <= startVal) {
                if (billableHoursEl) billableHoursEl.innerText = '--';
                if (estimatedFeeEl) estimatedFeeEl.innerText = '₹0.00';
                if (durationTextEl) durationTextEl.innerText = 'Invalid time selection (End must be after Start)';
                return;
            }

            var diffMs = endVal - startVal;
            var totalHours = diffMs / (1000 * 60 * 60);
            var billableHours = Math.ceil(totalHours);
            if (billableHours < 1) billableHours = 1;

            var estimatedFee = billableHours * hourlyRate;

            var totalMinutes = Math.floor(diffMs / (1000 * 60));
            var displayH = Math.floor(totalMinutes / 60);
            var displayM = totalMinutes % 60;

            if (billableHoursEl) billableHoursEl.innerText = billableHours + ' hr' + (billableHours > 1 ? 's' : '');
            if (estimatedFeeEl) estimatedFeeEl.innerText = '₹' + estimatedFee.toFixed(2);
            if (durationTextEl) durationTextEl.innerText = displayH + ' hr ' + displayM + ' mins (Rounded up to ' + billableHours + ' hrs)';
        }

        startInput.addEventListener('change', recalculateFee);
        endInput.addEventListener('change', recalculateFee);
        recalculateFee();
    }
});

// 4. Print Booking Receipt helper
function printParkingPermit() {
    window.print();
}
