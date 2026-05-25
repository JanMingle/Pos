function updateTimer() {

    const now = new Date();
    let year = now.getFullYear();

    // Target date (ex: Dec 11th 12AM)
    let future = new Date(year, 11, 11, 0, 0, 0); 

    // If target date passed, uses next year same date
    if (now > future) {
        future = new Date(year + 1, 11, 11, 0, 0, 0);
    }

    const diff = future - now;

    const days = Math.floor(diff / (1000 * 60 * 60 * 24));
    const hours = Math.floor(diff / (1000 * 60 * 60));
    const mins = Math.floor(diff / (1000 * 60));
    const secs = Math.floor(diff / 1000);

    const d = days;
    const h = hours - days * 24;
    const m = mins - hours * 60;
    const s = secs - mins * 60;

    document.getElementById("timer").innerHTML =
        '<div class="col-xxl-3 col-xl-6 col-lg-6 col-md-3 col-sm-3 col-6"><div class="p-3 coming-soon-time rounded"><p class="mb-1 fs-12 op-5">DAYS</p><h4 class="fw-semibold mb-0">' + d + '</h4></div></div>' +
        '<div class="col-xxl-3 col-xl-6 col-lg-6 col-md-3 col-sm-3 col-6"><div class="p-3 coming-soon-time rounded"><p class="mb-1 fs-12 op-5">HOURS</p><h4 class="fw-semibold mb-0">' + h + '</h4></div></div>' +
        '<div class="col-xxl-3 col-xl-6 col-lg-6 col-md-3 col-sm-3 col-6"><div class="p-3 coming-soon-time rounded"><p class="mb-1 fs-12 op-5">MINUTES</p><h4 class="fw-semibold mb-0">' + m + '</h4></div></div>' +
        '<div class="col-xxl-3 col-xl-6 col-lg-6 col-md-3 col-sm-3 col-6"><div class="p-3 coming-soon-time rounded"><p class="mb-1 fs-12 op-5">SECONDS</p><h4 class="fw-semibold mb-0">' + s + '</h4></div></div>';
}

setInterval(updateTimer, 1000);