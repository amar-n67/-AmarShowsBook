let isEmailVerified = false;
let isMobileVerified = false;
// ================= REGEX =================
const emailRegex = /^[a-zA-Z0-9._%+-]+@(gmail\.com|outlook\.com)$/;
const mobileRegex = /^[0-9]{10}$/;

// =====================================================
// 🔐 EMAIL
// =====================================================

function editEmail(oldVal) {
    let field = document.getElementById("emailField");

    field.removeAttribute("readonly");
    field.value = "";

    document.getElementById("emailCancel").classList.remove("d-none");
    document.getElementById("emailOtpSection").classList.remove("d-none");

    // hide old tick
    document.getElementById("emailVerifiedTick")?.classList.add("d-none");
}

function cancelEmail(oldVal) {
    let field = document.getElementById("emailField");

    field.value = oldVal;
    field.setAttribute("readonly", true);

    document.getElementById("emailCancel").classList.add("d-none");
    document.getElementById("emailOtpSection").classList.add("d-none");
    document.getElementById("emailVerifyBox").classList.add("d-none");

    document.getElementById("emailOtpInput").value = "";
}

function sendEmailOtp(oldEmail) {
    let newEmail = document.getElementById("emailField").value;

    if (!emailRegex.test(newEmail)) {
        alert("Invalid email ❌");
        return;
    }

    if (newEmail === oldEmail) {
        alert("Email must be different ❌");
        return;
    }

    fetch('/Otp/SendEmailOtp', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: `email=${encodeURIComponent(newEmail)}`
    })
    .then(res => res.json())
    .then(res => {
        if (res.success) {
            document.getElementById("emailVerifyBox").classList.remove("d-none");
            alert("OTP sent to email 📩");
        }
    });
}

function verifyEmailOtp() {
    let email = document.getElementById("emailField").value;
    let otp = document.getElementById("emailOtpInput").value;

    fetch('/Otp/VerifyEmailOtp', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: `email=${encodeURIComponent(email)}&otp=${encodeURIComponent(otp)}`
    })
    .then(res => res.json())
    .then(res => {
        if (res.success) {
            alert("Email verified ✅");

            document.getElementById("emailField").setAttribute("readonly", true);
            document.getElementById("emailOtpInput").setAttribute("readonly", true);

            document.getElementById("emailOtpSection").classList.add("d-none");
            document.getElementById("emailVerifyBox").classList.add("d-none");

            document.getElementById("emailVerifiedIcon").classList.remove("d-none");
        }
    });
}

// =====================================================
// 📱 MOBILE
// =====================================================

function editMobile(oldVal) {
    let field = document.getElementById("mobileField");

    field.removeAttribute("readonly");
    field.value = "";

    document.getElementById("mobileCancel").classList.remove("d-none");
    document.getElementById("mobileOtpSection").classList.remove("d-none");

    document.getElementById("mobileVerifiedTick")?.classList.add("d-none");
}

function cancelMobile(oldVal) {
    let field = document.getElementById("mobileField");

    field.value = oldVal;
    field.setAttribute("readonly", true);

    document.getElementById("mobileCancel").classList.add("d-none");
    document.getElementById("mobileOtpSection").classList.add("d-none");
    document.getElementById("mobileVerifyBox").classList.add("d-none");

    document.getElementById("mobileOtpInput").value = "";
}

function sendMobileOtp(oldMobile) {
    let newMobile = document.getElementById("mobileField").value;

    if (!mobileRegex.test(newMobile)) {
        alert("Invalid mobile ❌");
        return;
    }

    if (newMobile === oldMobile) {
        alert("Mobile must be different ❌");
        return;
    }

    fetch('/Otp/SendMobileOtp', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: `mobile=${encodeURIComponent(newMobile)}`
    })
    .then(res => res.json())
    .then(res => {
        if (res.success) {
            document.getElementById("mobileVerifyBox").classList.remove("d-none");
            alert("OTP sent to mobile 📱");
        }
    });
}

function verifyMobileOtp() {
    let mobile = document.getElementById("mobileField").value;
    let otp = document.getElementById("mobileOtpInput").value;

    fetch('/Otp/VerifyMobileOtp', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: `mobile=${encodeURIComponent(mobile)}&otp=${encodeURIComponent(otp)}`
    })
    .then(res => res.json())
    .then(res => {
        if (res.success) {
            alert("Mobile verified ✅");

            document.getElementById("mobileField").setAttribute("readonly", true);
            document.getElementById("mobileOtpInput").setAttribute("readonly", true);

            document.getElementById("mobileOtpSection").classList.add("d-none");
            document.getElementById("mobileVerifyBox").classList.add("d-none");

            document.getElementById("mobileVerifiedIcon").classList.remove("d-none");
        }
    });
}

// =====================================================
// 🏠 ADDRESS
// =====================================================

function editAddress(oldVal) {
    let field = document.getElementById("addressField");

    field.removeAttribute("readonly");
    field.value = "";

    document.getElementById("addressCancel").classList.remove("d-none");
    document.getElementById("addressDropdown").classList.remove("d-none");
}

function cancelAddress(oldVal) {
    let field = document.getElementById("addressField");

    field.value = oldVal;
    field.setAttribute("readonly", true);

    document.getElementById("addressCancel").classList.add("d-none");
    document.getElementById("addressDropdown").classList.add("d-none");
}

// =====================================================
// 🎬 DROPDOWN
// =====================================================

function enableSelect(id) {
    document.getElementById(id).removeAttribute("disabled");
}

// =====================================================
// 🖼 IMAGE
// =====================================================

let oldImageSrc = "";

function changeImage() {
    let fileInput = document.getElementById("profileImage");

    oldImageSrc = document.getElementById("profilePreview").src;

    fileInput.classList.remove("d-none");
    fileInput.click();

    document.getElementById("imageCancel").classList.remove("d-none");

    fileInput.onchange = function () {
        let file = fileInput.files[0];
        if (file) {
            let reader = new FileReader();
            reader.onload = function (e) {
                document.getElementById("profilePreview").src = e.target.result;
            };
            reader.readAsDataURL(file);
        }
    };
}

function cancelImage() {
    document.getElementById("profilePreview").src = oldImageSrc;
    document.getElementById("imageCancel").classList.add("d-none");
}
function enableSelect(id, cancelBtn) {
    let el = document.getElementById(id);

    el.removeAttribute("disabled");
    el.focus();

    document.getElementById(cancelBtn).classList.remove("d-none");
}

function cancelSelect(id, value, cancelBtn) {
    let el = document.getElementById(id);

    el.value = value;
    el.setAttribute("disabled", true);

    document.getElementById(cancelBtn).classList.add("d-none");
}
function validateProfileForm() {

    let email = document.getElementById("emailField").value.trim();
    let mobile = document.getElementById("mobileField").value.trim();

    let oldEmail = document.getElementById("originalEmail").value;
    let oldMobile = document.getElementById("originalMobile").value;

    let address = document.getElementById("addressField").value;
    let oldAddress = document.getElementById("originalAddress").value;

    let genre = document.getElementById("genreField").value;
    let oldGenre = document.getElementById("originalGenre").value;

    let language = document.getElementById("languageField").value;
    let oldLanguage = document.getElementById("originalLanguage").value;

    let image = document.getElementById("profileImage").value;

    // ================= EMPTY CHECK =================
    if (!email || !mobile) {
        showPopup("🚫 Hero bina identity ke nahi chalta... Email & Mobile zaroori hai!");
        return false;
    }

    if (!emailRegex.test(email)) {
        showPopup("Only @gmail.com or @outlook.com email is allowed.");
        return false;
    }

    // ================= CHANGE CHECK =================
    let isChanged =
        email !== oldEmail ||
        mobile !== oldMobile ||
        address !== oldAddress ||
        genre !== oldGenre ||
        language !== oldLanguage ||
        image !== "";

    if (!isChanged) {
        showPopup("🎬 Picture shuru hone se pehle hi khatam? Kuch toh change karo boss!");
        return false;
    }

    return true;
}
function showPopup(message) {
    alert(message);
}
function buildFullAddress() {

    let country = document.querySelector('[name="Country"]').value;
    let state = document.querySelector('[name="State"]').value;
    let district = document.querySelector('[name="District"]').value;
    let pincode = document.querySelector('[name="Pincode"]').value;

    let fullAddress = `${district}, ${state}, ${country} - ${pincode}`;

    document.getElementById("addressField").value = fullAddress;
}
