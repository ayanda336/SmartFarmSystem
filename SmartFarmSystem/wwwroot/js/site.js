// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Sidebar Toggle for Desktop
$('#sidebarToggle').on('click', function () {
    $('#sidebar').toggleClass('collapsed');
});

// Sidebar Toggle for Mobile
$('#sidebarToggleMobile').on('click', function () {
    $('#sidebar').toggleClass('active');
});

// Close sidebar when clicking outside on mobile
$(document).on('click', function (e) {
    if ($(window).width() <= 992) {
        if (!$(e.target).closest('#sidebar').length && !$(e.target).closest('#sidebarToggleMobile').length) {
            $('#sidebar').removeClass('active');
        }
    }
});