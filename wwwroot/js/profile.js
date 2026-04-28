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

    isEmailVerified = false;
    field.removeAttribute("readonly");
    field.value = "";

    document.getElementById("emailVerifiedIcon").classList.add("d-none");
    document.getElementById("emailCancel").classList.remove("d-none");
    document.getElementById("emailOtpSection").classList.remove("d-none");

    // hide old tick
    document.getElementById("emailVerifiedTick")?.classList.add("d-none");
}

function cancelEmail(oldVal) {
    let field = document.getElementById("emailField");

    isEmailVerified = false;
    field.value = oldVal;
    field.setAttribute("readonly", true);

    document.getElementById("emailCancel").classList.add("d-none");
    document.getElementById("emailOtpSection").classList.add("d-none");
    document.getElementById("emailVerifyBox").classList.add("d-none");

    document.getElementById("emailOtpInput").value = "";
    document.getElementById("emailOtpInput").removeAttribute("readonly");
    document.getElementById("emailVerifiedIcon").classList.add("d-none");
}

function sendEmailOtp(oldEmail) {
    let newEmail = document.getElementById("emailField").value.trim();

    if (!emailRegex.test(newEmail)) {
        alert("Only @gmail.com or @outlook.com email is allowed.");
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
            alert(res.devOtp ? `OTP sent. Development OTP: ${res.devOtp}` : "OTP sent to email 📩");
        } else {
            alert(res.message || "Email OTP could not be sent.");
        }
    })
    .catch(() => {
        alert("Email OTP could not be sent. Please try again.");
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
            isEmailVerified = true;
            alert("Email verified ✅");

            document.getElementById("emailField").setAttribute("readonly", true);
            document.getElementById("emailOtpInput").setAttribute("readonly", true);

            document.getElementById("emailOtpSection").classList.add("d-none");
            document.getElementById("emailVerifyBox").classList.add("d-none");

            document.getElementById("emailVerifiedIcon").classList.remove("d-none");
        } else {
            alert("Invalid email OTP.");
        }
    })
    .catch(() => {
        alert("Email OTP verification failed. Please try again.");
    });
}

// =====================================================
// 📱 MOBILE
// =====================================================

function editMobile(oldVal) {
    let field = document.getElementById("mobileField");

    isMobileVerified = false;
    field.removeAttribute("readonly");
    field.value = "";

    document.getElementById("mobileVerifiedIcon").classList.add("d-none");
    document.getElementById("mobileCancel").classList.remove("d-none");
    document.getElementById("mobileOtpSection").classList.remove("d-none");

    document.getElementById("mobileVerifiedTick")?.classList.add("d-none");
}

function cancelMobile(oldVal) {
    let field = document.getElementById("mobileField");

    isMobileVerified = false;
    field.value = oldVal;
    field.setAttribute("readonly", true);

    document.getElementById("mobileCancel").classList.add("d-none");
    document.getElementById("mobileOtpSection").classList.add("d-none");
    document.getElementById("mobileVerifyBox").classList.add("d-none");

    document.getElementById("mobileOtpInput").value = "";
    document.getElementById("mobileOtpInput").removeAttribute("readonly");
    document.getElementById("mobileVerifiedIcon").classList.add("d-none");
}

function sendMobileOtp(oldMobile) {
    let newMobile = document.getElementById("mobileField").value.trim();

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
            alert(res.devOtp ? `OTP sent. Development OTP: ${res.devOtp}` : "OTP sent to mobile 📱");
        } else {
            alert(res.message || "Mobile OTP could not be sent.");
        }
    })
    .catch(() => {
        alert("Mobile OTP could not be sent. Please try again.");
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
            isMobileVerified = true;
            alert("Mobile verified ✅");

            document.getElementById("mobileField").setAttribute("readonly", true);
            document.getElementById("mobileOtpInput").setAttribute("readonly", true);

            document.getElementById("mobileOtpSection").classList.add("d-none");
            document.getElementById("mobileVerifyBox").classList.add("d-none");

            document.getElementById("mobileVerifiedIcon").classList.remove("d-none");
        } else {
            alert("Invalid mobile OTP.");
        }
    })
    .catch(() => {
        alert("Mobile OTP verification failed. Please try again.");
    });
}

// =====================================================
// 🏠 ADDRESS
// =====================================================

function editAddress(oldVal) {
    let field = document.getElementById("addressField");

    field.removeAttribute("readonly");
    document.querySelectorAll(".address-part").forEach(el => {
        el.removeAttribute("disabled");
        el.removeAttribute("readonly");
        el.style.backgroundColor = "#fff";
        el.style.color = "#000";
        el.style.borderColor = "#ccc";
    });

    document.getElementById("addressCancel").classList.remove("d-none");
    document.querySelectorAll(".dropdown-icon").forEach(icon => icon.classList.remove("d-none"));
}

function cancelAddress(oldVal) {
    let field = document.getElementById("addressField");

    field.value = oldVal;
    field.setAttribute("readonly", true);

    document.getElementById("countryField").value = document.getElementById("originalCountry").value;
    document.getElementById("stateField").value = document.getElementById("originalState").value;
    document.getElementById("districtField").value = document.getElementById("originalDistrict").value;
    document.getElementById("pincodeField").value = document.getElementById("originalPincode").value;

    document.querySelectorAll("#countryField, #stateField, #districtField").forEach(el => {
        el.setAttribute("disabled", true);
        el.removeAttribute("style");
    });

    let pincode = document.getElementById("pincodeField");
    pincode.setAttribute("readonly", true);
    pincode.removeAttribute("style");

    document.getElementById("addressCancel").classList.add("d-none");
    document.querySelectorAll(".dropdown-icon").forEach(icon => icon.classList.add("d-none"));
}

// =====================================================
// 🎬 DROPDOWN
// =====================================================

function enableProfileSelect(id) {
    let el = document.getElementById(id);
    let wrapper = el.closest(".position-relative");

    el.removeAttribute("disabled");
    el.style.backgroundColor = "#fff";
    el.style.color = "#000";
    el.style.borderColor = "#ccc";
    wrapper?.querySelector(".dropdown-icon")?.classList.remove("d-none");
    el.focus();
}

function cancelSelect(id, value, cancelBtn) {
    let el = document.getElementById(id);

    el.value = value;
    el.setAttribute("disabled", true);

    document.getElementById(cancelBtn).classList.add("d-none");
}

// =====================================================
// 🖼 IMAGE
// =====================================================

let oldImageSrc = "";

function prepareImageChange() {
    if (!oldImageSrc) {
        oldImageSrc = document.getElementById("profilePreview").src;
    }

    document.getElementById("imageCancel").classList.remove("d-none");
}

function changeImage() {
    prepareImageChange();
    document.getElementById("profileImage").click();
}

function previewProfileImage() {
    let fileInput = document.getElementById("profileImage");
    let file = fileInput.files[0];

    if (!file) {
        return;
    }

    let reader = new FileReader();
    reader.onload = function (e) {
        document.getElementById("profilePreview").src = e.target.result;
    };
    reader.readAsDataURL(file);
}

function cancelImage() {
    document.getElementById("profilePreview").src = oldImageSrc;
    document.getElementById("profileImage").value = "";
    document.getElementById("imageCancel").classList.add("d-none");
    oldImageSrc = "";
}

function validateProfileForm() {

    let email = document.getElementById("emailField").value.trim();
    let mobile = document.getElementById("mobileField").value.trim();

    let oldEmail = document.getElementById("originalEmail").value;
    let oldMobile = document.getElementById("originalMobile").value;

    let address = document.getElementById("addressField").value;
    let oldAddress = document.getElementById("originalAddress").value;

    let country = document.getElementById("countryField").value;
    let oldCountry = document.getElementById("originalCountry").value;

    let state = document.getElementById("stateField").value;
    let oldState = document.getElementById("originalState").value;

    let district = document.getElementById("districtField").value;
    let oldDistrict = document.getElementById("originalDistrict").value;

    let pincode = document.getElementById("pincodeField").value;
    let oldPincode = document.getElementById("originalPincode").value;

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

    if (!mobileRegex.test(mobile)) {
        showPopup("Invalid mobile. Mobile must be exactly 10 digits.");
        return false;
    }

    if (email !== oldEmail && !isEmailVerified) {
        showPopup("Please verify the new email OTP before saving.");
        return false;
    }

    if (mobile !== oldMobile && !isMobileVerified) {
        showPopup("Please verify the new mobile OTP before saving.");
        return false;
    }

    // ================= CHANGE CHECK =================
    let isChanged =
        email !== oldEmail ||
        mobile !== oldMobile ||
        address !== oldAddress ||
        country !== oldCountry ||
        state !== oldState ||
        district !== oldDistrict ||
        pincode !== oldPincode ||
        genre !== oldGenre ||
        language !== oldLanguage ||
        image !== "";

    if (!isChanged) {
        showPopup("There is nothing to update.");
        return false;
    }

    document.getElementById("genreField").removeAttribute("disabled");
    document.getElementById("languageField").removeAttribute("disabled");
    document.getElementById("countryField").removeAttribute("disabled");
    document.getElementById("stateField").removeAttribute("disabled");
    document.getElementById("districtField").removeAttribute("disabled");

    return true;
}
function showPopup(message) {
    alert(message);
}
function buildFullAddress() {

    let country = document.getElementById("countryField").value;
    let state = document.getElementById("stateField").value;
    let district = document.getElementById("districtField").value;
    let pincode = document.getElementById("pincodeField").value;

    let location = [district, state, country].filter(Boolean).join(", ");
    let fullAddress = pincode ? `${location} - ${pincode}` : location;

    document.getElementById("addressField").value = fullAddress;
}
