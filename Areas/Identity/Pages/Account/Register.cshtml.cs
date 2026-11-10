// Licencirano pod .NET Foundation ugovorom.
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using ESportskiCentar.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace ESportskiCentar.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<Korisnik> _signInManager;
        private readonly UserManager<Korisnik> _userManager;
        private readonly IUserStore<Korisnik> _userStore;
        private readonly IUserEmailStore<Korisnik> _emailStore;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            UserManager<Korisnik> userManager,
            IUserStore<Korisnik> userStore,
            SignInManager<Korisnik> signInManager,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Ime je obavezno polje.")]
            [StringLength(30, MinimumLength = 2, ErrorMessage = "Ime mora imati između 2 i 30 karaktera.")]
            [RegularExpression(@"^[A-Za-zČčĆćŽžŠšĐđ]{2,30}$", ErrorMessage = "Ime može sadržavati samo slova (bez razmaka i crtica).")]
            [Display(Name = "Ime")]
            public string Ime { get; set; }

            [Required(ErrorMessage = "Prezime je obavezno polje.")]
            [StringLength(30, MinimumLength = 2, ErrorMessage = "Prezime mora imati između 2 i 30 karaktera.")]
            [RegularExpression(@"^[A-Za-zČčĆćŽžŠšĐđ]{2,30}$", ErrorMessage = "Prezime može sadržavati samo slova (bez razmaka i crtica).")]
            [Display(Name = "Prezime")]
            public string Prezime { get; set; }

            [Required(ErrorMessage = "Email adresa je obavezna.")]
            [EmailAddress(ErrorMessage = "Unesite validnu email adresu.")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Lozinka je obavezna.")]
            [StringLength(100, ErrorMessage = "Lozinka mora imati najmanje {2} karaktera.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Lozinka")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Potvrdi lozinku")]
            [Compare("Password", ErrorMessage = "Lozanke se ne podudaraju.")]
            public string ConfirmPassword { get; set; }
        }
        public async Task<IActionResult> OnGetAsync(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                string prethodnaStranica = Request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(prethodnaStranica))
                {
                    return Redirect(prethodnaStranica);
                }
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var user = CreateUser();
                user.ime = Input.Ime;
                user.prezime = Input.Prezime;
                user.EmailConfirmed = true; // receno da postavimo EmailConfirmed = true

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Korisnik");
                    _logger.LogInformation("Korisnik je uspješno kreirao novi nalog.");

                    await _signInManager.SignInAsync(user, isPersistent: false);

                    return LocalRedirect(returnUrl);
                }
                foreach (var error in result.Errors)
                {
                    string lokalizovanaGreska = error.Description;
                    if (error.Code == "DuplicateUserName" || error.Code == "DuplicateEmail")
                    {
                        lokalizovanaGreska = "Korisnik sa ovom email adresom već postoji.";
                    }
                    else if (error.Code == "PasswordTooShort")
                    {
                        lokalizovanaGreska = "Lozinka je prekratka.";
                    }

                    ModelState.AddModelError(string.Empty, lokalizovanaGreska);
                }
            }
            return Page();
        }

        private Korisnik CreateUser()
        {
            try
            {
                return Activator.CreateInstance<Korisnik>();
            }
            catch
            {
                throw new InvalidOperationException($"Nije moguće kreirati instancu klase '{nameof(Korisnik)}'.");
            }
        }

        private IUserEmailStore<Korisnik> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("Podrazumijevani UI zahtijeva user store sa podrškom za email.");
            }
            return (IUserEmailStore<Korisnik>)_userStore;
        }
    }
}