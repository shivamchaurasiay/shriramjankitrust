 
function openPopup() {
    document.body.classList.add('no-scroll');
}
 
function closePopup() {
    document.body.classList.remove('no-scroll');
    var notification = document.getElementById('notification');
    notification.style.display = 'none'; 
}
 
window.addEventListener('scroll', function () {
    var notification = document.getElementById('notification');
    if (notification.style.display === 'block') {
        closePopup();  
    }
});
