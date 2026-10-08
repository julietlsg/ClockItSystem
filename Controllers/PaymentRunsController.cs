using ClockItSystem.Data;
using ClockItSystem.Interfaces;
using ClockItSystem.Models;
using ClockItSystem.Models.Enums;
using ClockItSystem.Services;
using ClockItSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ClockItSystem.Controllers
{
    [Authorize(Roles = "Admin,Project Manager,CEO")]
    public class PaymentRunsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NetcashBatchGenerator _netcashBatchGenerator;
        private readonly NetcashPaymentService _netcashPaymentService;
        private readonly IClientAccessService _clientAccessService;

        public PaymentRunsController(
            ApplicationDbContext context,
            NetcashBatchGenerator netcashBatchGenerator,
            NetcashPaymentService netcashPaymentService,
            IClientAccessService clientAccessService
            )
        {
            _context = context;
            _netcashBatchGenerator = netcashBatchGenerator;
            _netcashPaymentService = netcashPaymentService;
            _clientAccessService = clientAccessService;
        }

        // ============================================================
        // PAYMENT RUN LIST
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var accessibleClientIds =
                await _clientAccessService
                    .GetAccessibleClientIdsAsync();

            var paymentRuns = await _context.StipendPaymentRuns
                .AsNoTracking()
                .Include(x => x.Client)
                .Where(x =>
                    accessibleClientIds.Contains(x.ClientId))
                .OrderByDescending(x => x.PeriodFrom)
                .ThenBy(x => x.Client.Name)
                .ToListAsync();

            return View(paymentRuns);
        }

        // ============================================================
        // CREATE PAYMENT RUN - FILTER PAGE
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new PaymentRunViewModel
            {
                PaymentDate = DateTime.Today,
                PaymentPeriod = new DateTime(
                    DateTime.Today.Year,
                    DateTime.Today.Month,
                    1)
            };

            await PopulateFilters(model);

            return View(model);
        }

        // ============================================================
        // CREATE PAYMENT RUN
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            PaymentRunViewModel model)
        {
            await PopulateFilters(model);

            if (!ModelState.IsValid)
                return View(model);

            if (!model.ClientId.HasValue ||
                !model.PaymentPeriod.HasValue ||
                !model.PaymentDate.HasValue)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Please select a client, payment period and payment date.");

                return View(model);
            }

            var paymentDate = model.PaymentDate.Value.Date;

            if (paymentDate.DayOfWeek == DayOfWeek.Saturday ||
                paymentDate.DayOfWeek == DayOfWeek.Sunday)
            {
                ModelState.AddModelError(
                    nameof(model.PaymentDate),
                    "The payment date must be a weekday.");

                return View(model);
            }

            // --------------------------------------------------------
            // SECURITY CHECK
            // --------------------------------------------------------

            if (!await _clientAccessService
                .CanAccessClientAsync(model.ClientId.Value))
            {
                return Forbid();
            }

            // --------------------------------------------------------
            // DETERMINE PAYMENT PERIOD
            // --------------------------------------------------------

            var periodFrom = new DateTime(
                model.PaymentPeriod.Value.Year,
                model.PaymentPeriod.Value.Month,
                1);

            var periodTo = periodFrom
                .AddMonths(1)
                .AddDays(-1);

            // Used when querying DateTime values so the entire
            // final day of the month is included.
            var periodEndExclusive = periodTo.AddDays(1);

            // --------------------------------------------------------
            // PREVENT DUPLICATE PAYMENT RUN
            // --------------------------------------------------------

            var existingRun = await _context.StipendPaymentRuns
                .AnyAsync(x =>
                    x.ClientId == model.ClientId.Value &&
                    x.PeriodFrom == periodFrom &&
                    x.PeriodTo == periodTo);

            if (existingRun)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "A payment run already exists for the selected client and payment period.");

                return View(model);
            }

            // --------------------------------------------------------
            // GET CLIENT
            // --------------------------------------------------------

            var client = await _context.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ClientId == model.ClientId.Value &&
                    x.IsActive);

            if (client == null)
            {
                ModelState.AddModelError(
                    nameof(model.ClientId),
                    "Selected client could not be found.");

                return View(model);
            }

            // --------------------------------------------------------
            // GET STIPEND RATE
            // --------------------------------------------------------

            var dailyRate = await GetDailyRateAsync(
                model.ClientId.Value,
                periodFrom,
                periodTo);

            if (!dailyRate.HasValue)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No active stipend rate was found for the selected client and payment period.");

                return View(model);
            }

            // --------------------------------------------------------
            // GET ACTIVE STUDENTS
            // --------------------------------------------------------

            var students = await _context.Students
                .AsNoTracking()
                .Where(s =>
                    s.ClientId == model.ClientId.Value &&
                    s.IsActive)
                .Include(s => s.Bank)
                .Include(s => s.BankBranch)
                .Include(s => s.AccountType)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            if (!students.Any())
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No active students were found for the selected client.");

                return View(model);
            }

            // --------------------------------------------------------
            // CREATE PAYMENT RUN
            // --------------------------------------------------------

            var paymentRun = new StipendPaymentRun
            {
                ClientId = client.ClientId,

                PaymentDate = model.PaymentDate.Value,

                PeriodFrom = periodFrom,

                PeriodTo = periodTo,

                Status = "Draft",

                CreatedBy =
                    User.Identity?.Name ?? "System",

                CreatedAt = DateTime.Now
            };

            _context.StipendPaymentRuns.Add(paymentRun);

            await _context.SaveChangesAsync();

            // --------------------------------------------------------
            // PROCESS EACH STUDENT
            // --------------------------------------------------------

            var paymentRows = new List<StipendPayment>();

            foreach (var student in students)
            {
                var payment =
                    await BuildStudentPaymentAsync(
                        student,
                        paymentRun.Id,
                        dailyRate.Value,
                        periodFrom,
                        periodEndExclusive);

                if (payment != null)
                {
                    paymentRows.Add(payment);
                }
            }

            // --------------------------------------------------------
            // SAVE PAYMENT ROWS
            // --------------------------------------------------------

            if (paymentRows.Any())
            {
                await _context.StipendPayments
                    .AddRangeAsync(paymentRows);

                paymentRun.TotalStudents =
                    paymentRows.Count;

                paymentRun.TotalEligibleDays =
                    paymentRows.Sum(
                        x => x.TotalEligibleDays);

                paymentRun.TotalAmount =
                    paymentRows.Sum(
                        x => x.StipendAmount);
            }
            else
            {
                paymentRun.TotalStudents = 0;
                paymentRun.TotalEligibleDays = 0;
                paymentRun.TotalAmount = 0;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = paymentRun.Id
                });
        }

        // ============================================================
        // PAYMENT RUN DETAILS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var paymentRun =
                await _context.StipendPaymentRuns
                    .AsNoTracking()
                    .Include(x => x.Client)
                    .Include(x => x.Payments)
                        .ThenInclude(x => x.Student)
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);

            if (paymentRun == null)
                return NotFound();

            // --------------------------------------------------------
            // SECURITY CHECK
            // --------------------------------------------------------

            if (!await _clientAccessService
                .CanAccessClientAsync(
                    paymentRun.ClientId))
            {
                return Forbid();
            }

            // --------------------------------------------------------
            // BUILD VIEW MODEL
            // --------------------------------------------------------

            var model =
                new PaymentRunResultViewModel
                {
                    PaymentRunId = paymentRun.Id,

                    ClientId = paymentRun.ClientId,

                    ClientName = paymentRun.Client?.Name
                        ?? string.Empty,

                    PaymentDate = paymentRun.PaymentDate,

                    PeriodFrom = paymentRun.PeriodFrom,

                    PeriodTo = paymentRun.PeriodTo,

                    TotalStudents =
                        paymentRun.TotalStudents,

                    TotalEligibleDays =
                        paymentRun.TotalEligibleDays,

                    TotalAmount =
                        paymentRun.TotalAmount,

                    Status =
                        paymentRun.Status,

                    Students =
                        paymentRun.Payments
                            .OrderBy(x =>
                                x.Student.LastName)
                            .ThenBy(x =>
                                x.Student.FirstName)
                            .Select(x =>
                                new PaymentRunStudentViewModel
                                {
                                    StudentId =
                                        x.StudentId,

                                    StudentNumber =
                                        x.Student.StudentNumber,

                                    StudentName =
                                        $"{x.Student.FirstName} {x.Student.LastName}",

                                    EligibleAttendanceDays =
                                        x.EligibleAttendanceDays,

                                    LeaveDays =
                                        x.LeaveDays,

                                    SickLeaveDays =
                                        x.SickLeaveDays,

                                    FamilyResponsibilityLeaveDays =
                                        x.FamilyResponsibilityLeaveDays,

                                    TotalEligibleDays =
                                        x.TotalEligibleDays,

                                    DailyRate =
                                        x.DailyRate,

                                    StipendAmount =
                                        x.StipendAmount,

                                    BankName =
                                        x.BankName,

                                    BranchCode =
                                        x.BranchCode,

                                    AccountNumber =
                                        x.AccountNumber,

                                    AccountType =
                                        x.AccountType,

                                    AccountHolderName =
                                        x.AccountHolderName,

                                    Status =
                                        x.Status
                                })
                            .ToList()
                };

            return View(model);
        }


        // ============================================================
        // SUBMIT PAYMENT RUN FOR REVIEW
        // ============================================================


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitForReview(int id)
        {
            var paymentRun = await _context.StipendPaymentRuns
                .Include(x => x.Payments)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (paymentRun == null)
                return NotFound();

            // Make sure the current user can access this client's payment run
            if (!await _clientAccessService.CanAccessClientAsync(paymentRun.ClientId))
                return Forbid();

            // Only Draft payment runs can be submitted
            if (paymentRun.Status != "Draft")
            {
                TempData["Error"] =
                    "Only a Draft payment run can be submitted for review.";

                return RedirectToAction(nameof(Details), new { id });
            }

            // There must be at least one payable student
            if (paymentRun.Payments == null || !paymentRun.Payments.Any())
            {
                TempData["Error"] =
                    "The payment run contains no payable students.";

                return RedirectToAction(nameof(Details), new { id });
            }

            // Move the payment run to the review stage
            paymentRun.Status = "PendingReview";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Payment run submitted for review successfully.";

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        [Authorize(Roles = "Admin,CEO")]
        public async Task<IActionResult> Review()
        {
            var paymentRuns = await _context.StipendPaymentRuns
                .AsNoTracking()
                .Include(x => x.Client)
                .Where(x => x.Status == "PendingReview")
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new PaymentReviewItemViewModel
                {
                    PaymentRunId = x.Id,
                    ClientName = x.Client.Name,
                    PeriodFrom = x.PeriodFrom,
                    PeriodTo = x.PeriodTo,
                    TotalStudents = x.TotalStudents,
                    TotalEligibleDays = x.TotalEligibleDays,
                    TotalAmount = x.TotalAmount,
                    Status = x.Status,
                    CreatedAt = x.CreatedAt,
                    CreatedBy = x.CreatedBy
                })
                .ToListAsync();

            var model = new PaymentReviewViewModel
            {
                PaymentRuns = paymentRuns
            };

            return View(model);
        }


        [HttpPost]
        [Authorize(Roles = "Admin,CEO")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApprovePaymentRun(int id)
        {
            var paymentRun = await _context.StipendPaymentRuns
                .Include(x => x.Payments)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (paymentRun == null)
                return NotFound();

            if (paymentRun.Status != "PendingReview")
            {
                TempData["Error"] =
                    "Only a payment run that is Pending Review can be approved.";

                return RedirectToAction(nameof(Details), new { id });
            }

            if (paymentRun.Payments == null || !paymentRun.Payments.Any())
            {
                TempData["Error"] =
                    "The payment run contains no payable students.";

                return RedirectToAction(nameof(Details), new { id });
            }

            // Validate banking details before approval
            var invalidPayments = paymentRun.Payments
                .Where(x =>
                    string.IsNullOrWhiteSpace(x.BankName) ||
                    string.IsNullOrWhiteSpace(x.BranchCode) ||
                    string.IsNullOrWhiteSpace(x.AccountNumber) ||
                    string.IsNullOrWhiteSpace(x.AccountType) ||
                    string.IsNullOrWhiteSpace(x.AccountHolderName))
                .ToList();

            if (invalidPayments.Any())
            {
                TempData["Error"] =
                    $"{invalidPayments.Count} payment(s) have incomplete banking details. " +
                    "Please correct the banking information before approving the payment run.";

                return RedirectToAction(nameof(Details), new { id });
            }

            var currentUser = User.Identity?.Name ?? "Unknown";

            // Approve the payment run
            paymentRun.Status = "Approved";
            paymentRun.ApprovedBy = currentUser;
            paymentRun.ApprovedAt = DateTime.Now;

            // Approve each payment within the run
            foreach (var payment in paymentRun.Payments)
            {
                payment.Status = "Approved";
                payment.FailureReason = null;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Payment run approved successfully.";

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,CEO")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectPaymentRun(
            int id,
            string rejectionReason)
        {
            var paymentRun = await _context.StipendPaymentRuns
                .Include(x => x.Payments)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (paymentRun == null)
                return NotFound();

            if (paymentRun.Status != "PendingReview")
            {
                TempData["Error"] =
                    "Only a payment run that is Pending Review can be rejected.";

                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                TempData["Error"] =
                    "Please provide a reason for rejecting the payment run.";

                return RedirectToAction(nameof(Details), new { id });
            }

            var currentUser = User.Identity?.Name ?? "Unknown";

            paymentRun.Status = "Rejected";
            paymentRun.ApprovedBy = currentUser;
            paymentRun.ApprovedAt = DateTime.Now;
            paymentRun.FailureReason = rejectionReason.Trim();

            foreach (var payment in paymentRun.Payments)
            {
                payment.Status = "Rejected";
                payment.FailureReason = rejectionReason.Trim();
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Payment run rejected successfully.";

            return RedirectToAction(nameof(Details), new { id });
        }

        #region NetCashIntegration

        // ============================================================
        // NetCash
        // ============================================================

        [HttpGet]
        [Authorize(Roles = "Admin,Project Manager,CEO")]
        public async Task<IActionResult> NetcashPreview(int id)
        {
            var paymentRun =
                await _context.StipendPaymentRuns
                    .AsNoTracking()
                    .Include(x => x.Client)
                    .Include(x => x.Payments)
                        .ThenInclude(x => x.Student)
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);

            if (paymentRun == null)
                return NotFound();

            // --------------------------------------------------------
            // ONLY APPROVED RUNS MAY BE PREPARED FOR NETCASH
            // --------------------------------------------------------

            if (paymentRun.Status != "Approved")
            {
                TempData["Error"] =
                    "Only an approved payment run can be prepared for Netcash.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            // --------------------------------------------------------
            // PAYMENTS MUST EXIST
            // --------------------------------------------------------

            if (paymentRun.Payments == null ||
                !paymentRun.Payments.Any())
            {
                TempData["Error"] =
                    "The payment run contains no payments.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            // --------------------------------------------------------
            // PASS NETCASH SUBMISSION INFORMATION TO THE VIEW
            // --------------------------------------------------------
            //
            // IMPORTANT:
            // A Netcash file token does NOT necessarily mean that the
            // batch was successfully loaded.
            //
            // The actual upload status comes from
            // RequestFileUploadReport().
            // --------------------------------------------------------

            ViewBag.NetcashFileToken =
                paymentRun.NetcashFileToken;

            ViewBag.NetcashUploadStatus =
                paymentRun.NetcashUploadStatus;

            ViewBag.NetcashUploadReport =
                paymentRun.NetcashUploadReport;

            ViewBag.NetcashReportedAt =
                paymentRun.NetcashReportedAt;

            ViewBag.SubmittedAt =
                paymentRun.SubmittedAt;

            // --------------------------------------------------------
            // BUILD PREVIEW
            // --------------------------------------------------------

            var preview = new NetcashBatchPreviewViewModel
            {
                PaymentRunId =
                    paymentRun.Id,

                ClientName =
                    paymentRun.Client.Name,

                PaymentDate =
                    paymentRun.PaymentDate,

                PeriodFrom =
                    paymentRun.PeriodFrom,

                PeriodTo =
                    paymentRun.PeriodTo,

                TotalPayments =
                    paymentRun.Payments.Count,

                TotalAmount =
                    paymentRun.Payments.Sum(
                        x => x.StipendAmount),

                Status =
                    paymentRun.Status
            };

            // --------------------------------------------------------
            // VALIDATE EACH PAYMENT
            // --------------------------------------------------------

            foreach (var payment in paymentRun.Payments)
            {
                var validationErrors =
                    new List<string>();

                // ----------------------------------------------------
                // ACCOUNT HOLDER NAME
                // ----------------------------------------------------

                if (string.IsNullOrWhiteSpace(
                    payment.AccountHolderName))
                {
                    validationErrors.Add(
                        "Account holder name is missing.");
                }
                else if (
                    payment.AccountHolderName.Trim().Length > 30)
                {
                    validationErrors.Add(
                        "Account holder name exceeds 30 characters.");
                }

                // ----------------------------------------------------
                // BANK NAME
                // ----------------------------------------------------

                if (string.IsNullOrWhiteSpace(
                    payment.BankName))
                {
                    validationErrors.Add(
                        "Bank name is missing.");
                }

                // ----------------------------------------------------
                // BRANCH CODE
                // ----------------------------------------------------

                if (string.IsNullOrWhiteSpace(
                    payment.BranchCode))
                {
                    validationErrors.Add(
                        "Branch code is missing.");
                }
                else if (!payment.BranchCode
                    .Trim()
                    .All(char.IsDigit))
                {
                    validationErrors.Add(
                        "Branch code must contain digits only.");
                }
                else if (
                    payment.BranchCode.Trim().Length > 6)
                {
                    validationErrors.Add(
                        "Branch code may not exceed 6 digits.");
                }

                // ----------------------------------------------------
                // ACCOUNT NUMBER
                // ----------------------------------------------------

                if (string.IsNullOrWhiteSpace(
                    payment.AccountNumber))
                {
                    validationErrors.Add(
                        "Account number is missing.");
                }
                else if (!payment.AccountNumber
                    .Trim()
                    .All(char.IsDigit))
                {
                    validationErrors.Add(
                        "Account number must contain digits only.");
                }
                else if (
                    payment.AccountNumber.Trim().Length > 11)
                {
                    validationErrors.Add(
                        "Account number may not exceed 11 digits.");
                }

                // ----------------------------------------------------
                // ACCOUNT TYPE
                // ----------------------------------------------------

                if (string.IsNullOrWhiteSpace(
                    payment.AccountType))
                {
                    validationErrors.Add(
                        "Account type is missing.");
                }
                else
                {
                    var accountType =
                        payment.AccountType
                            .Trim()
                            .ToLowerInvariant();

                    var supportedAccountType =
                        accountType is
                            "1"
                            or "2"
                            or "3"
                            or "9"
                            or "current"
                            or "checking"
                            or "current/checking"
                            or "cheque"
                            or "cheque account"
                            or "savings"
                            or "saving"
                            or "transmission"
                            or "public recipient"
                            or "public beneficiary";

                    if (!supportedAccountType)
                    {
                        validationErrors.Add(
                            $"Unsupported account type '{payment.AccountType}'.");
                    }
                }

                // ----------------------------------------------------
                // PAYMENT AMOUNT
                // ----------------------------------------------------

                if (payment.StipendAmount <= 0)
                {
                    validationErrors.Add(
                        "Payment amount must be greater than zero.");
                }

                // ----------------------------------------------------
                // STUDENT NUMBER / PAYMENT REFERENCE
                // ----------------------------------------------------

                if (string.IsNullOrWhiteSpace(
                    payment.Student.StudentNumber))
                {
                    validationErrors.Add(
                        "Student number is required.");
                }
                else
                {
                    var paymentReference =
                        $"CLOCKIT/{payment.Student.StudentNumber}/{paymentRun.PeriodFrom:yyyyMM}";

                    if (paymentReference.Length > 20)
                    {
                        validationErrors.Add(
                            "Payment reference exceeds the Netcash maximum of 20 characters.");
                    }
                }

                // ----------------------------------------------------
                // VALIDATION RESULT
                // ----------------------------------------------------

                var valid =
                    !validationErrors.Any();

                // ----------------------------------------------------
                // ADD PAYMENT TO PREVIEW
                // ----------------------------------------------------

                preview.Payments.Add(
                    new NetcashBatchPreviewItemViewModel
                    {
                        PaymentId =
                            payment.Id,

                        StudentId =
                            payment.StudentId,

                        StudentNumber =
                            payment.Student.StudentNumber,

                        StudentName =
                            payment.Student.FirstName +
                            " " +
                            payment.Student.LastName,

                        AccountHolderName =
                            payment.AccountHolderName ??
                            string.Empty,

                        BankName =
                            payment.BankName ??
                            string.Empty,

                        BranchCode =
                            payment.BranchCode ??
                            string.Empty,

                        AccountNumber =
                            payment.AccountNumber ??
                            string.Empty,

                        AccountType =
                            payment.AccountType ??
                            string.Empty,

                        Amount =
                            payment.StipendAmount,

                        PaymentReference =
                            $"CLOCKIT/{payment.Student.StudentNumber}/{paymentRun.PeriodFrom:yyyyMM}",

                        BankingDetailsValid =
                            valid,

                        ValidationMessage =
                            valid
                                ? null
                                : string.Join(
                                    " ",
                                    validationErrors)
                    });
            }

            return View(preview);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Project Manager,CEO")]
        public async Task<IActionResult> DownloadNetcashBatch(int id)
        {
            var paymentRun =
                await _context.StipendPaymentRuns
                    .AsNoTracking()
                    .Include(x => x.Client)
                    .Include(x => x.Payments)
                        .ThenInclude(x => x.Student)
                    .FirstOrDefaultAsync(x => x.Id == id);

            if (paymentRun == null)
                return NotFound();

            if (paymentRun.Status != "Approved")
            {
                TempData["Error"] =
                    "Only an approved payment run can generate a Netcash batch.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (paymentRun.Payments == null ||
                !paymentRun.Payments.Any())
            {
                TempData["Error"] =
                    "The payment run contains no payments.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            try
            {
                // ====================================================
                // STAGE 1 - REVIEW ONLY
                // ====================================================
                // This download does NOT submit anything to Netcash.
                //
                // The real Netcash Salary/Creditor Service Key will
                // be configured securely before Stage 2.
                // ====================================================

                const string reviewServiceKey =
                    "{{NETCASH_SALARY_SERVICE_KEY}}";

                var fileContent =
                    _netcashBatchGenerator.Generate(
                        paymentRun,
                        reviewServiceKey);

                var safeClientName =
                    new string(
                        paymentRun.Client.Name
                            .Where(char.IsLetterOrDigit)
                            .ToArray());

                if (string.IsNullOrWhiteSpace(safeClientName))
                    safeClientName = "Client";

                var fileName =
                    $"ClockIT_Netcash_{safeClientName}_{paymentRun.PaymentDate:yyyyMMdd}.txt";

                return File(
                    Encoding.UTF8.GetBytes(fileContent),
                    "text/plain",
                    fileName);
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] =
                    $"Netcash batch validation failed: {ex.Message}";

                return RedirectToAction(
                    nameof(NetcashPreview),
                    new { id });
            }
        }

        // SUBMIT NETCASH BATCH
        [HttpPost]
        [Authorize(Roles = "Admin,Project Manager,CEO")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitNetcashBatch(int id)
        {
            var paymentRun =
                await _context.StipendPaymentRuns
                    .Include(x => x.Client)
                    .Include(x => x.Payments)
                        .ThenInclude(x => x.Student)
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);

            if (paymentRun == null)
                return NotFound();

            // --------------------------------------------------------
            // ONLY APPROVED RUNS MAY BE SUBMITTED
            // --------------------------------------------------------

            if (paymentRun.Status != "Approved")
            {
                TempData["Error"] =
                    "Only an approved payment run can be submitted to Netcash.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            // --------------------------------------------------------
            // PAYMENTS MUST EXIST
            // --------------------------------------------------------

            if (paymentRun.Payments == null ||
                !paymentRun.Payments.Any())
            {
                TempData["Error"] =
                    "The payment run contains no payments.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            // --------------------------------------------------------
            // SUCCESSFUL SUBMISSIONS MUST NEVER BE RESUBMITTED
            // --------------------------------------------------------

            if (string.Equals(
                    paymentRun.NetcashUploadStatus,
                    "Successful",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "This payment run has already been successfully submitted to Netcash.";

                return RedirectToAction(
                    nameof(NetcashPreview),
                    new { id });
            }

            // --------------------------------------------------------
            // SUCCESSFUL WITH ERRORS ALSO REQUIRES REVIEW
            // --------------------------------------------------------

            if (string.Equals(
                    paymentRun.NetcashUploadStatus,
                    "SuccessfulWithErrors",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Netcash processed this batch with errors. " +
                    "Please review the Netcash upload report before proceeding.";

                return RedirectToAction(
                    nameof(NetcashPreview),
                    new { id });
            }

            try
            {
                // ----------------------------------------------------
                // SUBMIT OR POLL NETCASH
                // ----------------------------------------------------
                //
                // NetcashPaymentService handles both scenarios:
                //
                // 1. No file token exists:
                //    Upload the batch.
                //
                // 2. File token already exists:
                //    DO NOT upload again.
                //    Poll the existing Netcash upload report.
                //
                // This prevents duplicate Netcash batches.
                // ----------------------------------------------------

                var result =
                    await _netcashPaymentService
                        .SubmitSalaryBatchAsync(paymentRun);

                // ----------------------------------------------------
                // SAVE NETCASH RESULT
                // ----------------------------------------------------

                paymentRun.NetcashFileToken =
                    string.IsNullOrWhiteSpace(result.FileToken)
                        ? paymentRun.NetcashFileToken
                        : result.FileToken.Trim();

                paymentRun.NetcashUploadStatus =
                    result.UploadStatus;

                paymentRun.NetcashUploadReport =
                    result.UploadReport;

                paymentRun.NetcashReportedAt =
                    result.ReportedAt;

                paymentRun.SubmittedAt ??=
                    DateTime.Now;

                // ----------------------------------------------------
                // SUCCESSFUL
                // ----------------------------------------------------

                if (string.Equals(
                        result.UploadStatus,
                        "Successful",
                        StringComparison.OrdinalIgnoreCase))
                {
                    paymentRun.FailureReason = null;

                    await _context.SaveChangesAsync();

                    TempData["Success"] =
                        "The Netcash batch was successfully uploaded and " +
                        "processed by Netcash.";

                    return RedirectToAction(
                        nameof(NetcashPreview),
                        new { id });
                }

                // ----------------------------------------------------
                // SUCCESSFUL WITH ERRORS
                // ----------------------------------------------------

                if (string.Equals(
                        result.UploadStatus,
                        "SuccessfulWithErrors",
                        StringComparison.OrdinalIgnoreCase))
                {
                    paymentRun.FailureReason =
                        "Netcash processed the batch with errors. " +
                        "Review the Netcash upload report.";

                    await _context.SaveChangesAsync();

                    TempData["Error"] =
                        "Netcash processed the batch with errors. " +
                        "Please review the upload report.";

                    return RedirectToAction(
                        nameof(NetcashPreview),
                        new { id });
                }

                // ----------------------------------------------------
                // UNSUCCESSFUL
                // ----------------------------------------------------

                if (string.Equals(
                        result.UploadStatus,
                        "Unsuccessful",
                        StringComparison.OrdinalIgnoreCase))
                {
                    paymentRun.FailureReason =
                        "Netcash rejected the batch. " +
                        "Review the Netcash upload report before retrying.";

                    await _context.SaveChangesAsync();

                    TempData["Error"] =
                        "Netcash rejected the batch. " +
                        "Please review the upload report before retrying.";

                    return RedirectToAction(
                        nameof(NetcashPreview),
                        new { id });
                }

                // ----------------------------------------------------
                // PENDING
                // ----------------------------------------------------

                if (string.Equals(
                        result.UploadStatus,
                        "Pending",
                        StringComparison.OrdinalIgnoreCase))
                {
                    paymentRun.FailureReason = null;

                    await _context.SaveChangesAsync();

                    TempData["Error"] =
                        "Netcash has received the batch, but the upload report " +
                        "is not ready yet. Please check the Netcash status again.";

                    return RedirectToAction(
                        nameof(NetcashPreview),
                        new { id });
                }

                // ----------------------------------------------------
                // UNKNOWN
                // ----------------------------------------------------

                paymentRun.FailureReason =
                    "ClockIT received a response from Netcash, " +
                    "but could not determine the final upload status.";

                await _context.SaveChangesAsync();

                TempData["Error"] =
                    "Netcash returned a response, but the upload status " +
                    "could not be determined. Please review the upload report.";

                return RedirectToAction(
                    nameof(NetcashPreview),
                    new { id });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] =
                    $"Netcash submission failed: {ex.Message}";

                return RedirectToAction(
                    nameof(NetcashPreview),
                    new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "An unexpected error occurred while communicating " +
                    $"with Netcash: {ex.Message}";

                return RedirectToAction(
                    nameof(NetcashPreview),
                    new { id });
            }
        }

        #endregion

        # region NetCashPaymentHistory

        [HttpGet]
        [Route("/PaymentHistory")]
        public async Task<IActionResult> PaymentHistory(
            int? clientId,
            string? status,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var accessibleClientIds =
                await _clientAccessService
                    .GetAccessibleClientIdsAsync();

            // --------------------------------------------------------
            // CLIENT FILTER
            // --------------------------------------------------------

            var clients = await _context.Clients
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    accessibleClientIds.Contains(x.ClientId))
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem
                {
                    Value = x.ClientId.ToString(),
                    Text = x.Name
                })
                .ToListAsync();

            // --------------------------------------------------------
            // PAYMENT HISTORY QUERY
            // --------------------------------------------------------

            var query = _context.StipendPaymentRuns
                .AsNoTracking()
                .Include(x => x.Client)
                .Where(x =>
                    accessibleClientIds.Contains(x.ClientId))
                .AsQueryable();

            // --------------------------------------------------------
            // CLIENT FILTER
            // --------------------------------------------------------

            if (clientId.HasValue)
            {
                query = query.Where(x =>
                    x.ClientId == clientId.Value);
            }

            // --------------------------------------------------------
            // STATUS FILTER
            // --------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x =>
                    x.Status == status);
            }

            // --------------------------------------------------------
            // FROM DATE
            // --------------------------------------------------------

            if (fromDate.HasValue)
            {
                var startDate = fromDate.Value.Date;

                query = query.Where(x =>
                    x.PaymentDate >= startDate);
            }

            // --------------------------------------------------------
            // TO DATE
            // --------------------------------------------------------

            if (toDate.HasValue)
            {
                var endDateExclusive =
                    toDate.Value.Date.AddDays(1);

                query = query.Where(x =>
                    x.PaymentDate < endDateExclusive);
            }

            // --------------------------------------------------------
            // BUILD RESULT
            // --------------------------------------------------------

            var paymentRuns = await query
                .OrderByDescending(x => x.PaymentDate)
                .ThenByDescending(x => x.CreatedAt)
                .Select(x => new PaymentHistoryItemViewModel
                {
                    PaymentRunId = x.Id,

                    ClientName =
                        x.Client != null
                            ? x.Client.Name
                            : string.Empty,

                    PaymentDate =
                        x.PaymentDate,

                    PeriodFrom =
                        x.PeriodFrom,

                    PeriodTo =
                        x.PeriodTo,

                    TotalStudents =
                        x.TotalStudents,

                    TotalEligibleDays =
                        x.TotalEligibleDays,

                    TotalAmount =
                        x.TotalAmount,

                    Status =
                        x.Status,

                    NetcashUploadStatus =
                        x.NetcashUploadStatus,

                    NetcashFileToken =
                        x.NetcashFileToken,

                    NetcashUploadReport =
                        x.NetcashUploadReport,

                    NetcashReportedAt =
                        x.NetcashReportedAt,

                    SubmittedAt =
                        x.SubmittedAt,

                    FailureReason =
                        x.FailureReason,

                    CreatedAt =
                        x.CreatedAt,

                    CreatedBy =
                        x.CreatedBy
                })
                .ToListAsync();

            // --------------------------------------------------------
            // VIEW MODEL
            // --------------------------------------------------------

            var model = new PaymentHistoryViewModel
            {
                ClientId = clientId,

                Status = status,

                FromDate = fromDate,

                ToDate = toDate,

                Clients = clients,

                PaymentRuns = paymentRuns
            };

            return View(model);
        }

        #endregion

        // ============================================================
        // POPULATE FILTERS
        // ============================================================

        private async Task PopulateFilters(
            PaymentRunViewModel model)
        {
            var accessibleClientIds =
                await _clientAccessService
                    .GetAccessibleClientIdsAsync();

            model.Clients =
                await _context.Clients
                    .Where(x =>
                        x.IsActive &&
                        accessibleClientIds.Contains(
                            x.ClientId))
                    .OrderBy(x => x.Name)
                    .Select(x =>
                        new SelectListItem
                        {
                            Value =
                                x.ClientId.ToString(),

                            Text =
                                x.Name
                        })
                    .ToListAsync();
        }

        // ============================================================
        // GET DAILY STIPEND RATE
        // ============================================================

        private async Task<decimal?> GetDailyRateAsync(
            int clientId,
            DateTime periodFrom,
            DateTime periodTo)
        {
            var rate =
                await _context.ClientStipendRates
                    .AsNoTracking()
                    .Where(x =>
                        x.ClientId == clientId &&
                        x.IsActive &&
                        x.EffectiveFrom <= periodTo &&
                        (!x.EffectiveTo.HasValue ||
                         x.EffectiveTo.Value >= periodFrom))
                    .OrderByDescending(
                        x => x.EffectiveFrom)
                    .FirstOrDefaultAsync();

            return rate?.DailyRate;
        }

        // BUILD STUDENT PAYMENT
        private async Task<StipendPayment?> BuildStudentPaymentAsync(
            Student student,
            int paymentRunId,
            decimal dailyRate,
            DateTime periodFrom,
            DateTime periodEndExclusive)
        {
            var attendanceRecords =
                await _context.AttendanceRecords
                    .AsNoTracking()
                    .Include(x =>
                        x.AttendanceApproval)
                    .Where(x =>
                        x.StudentId == student.Id &&
                        x.ClientId == student.ClientId &&
                        x.AttendanceDate >= periodFrom &&
                        x.AttendanceDate < periodEndExclusive)
                    .ToListAsync();

            // APPROVED ATTENDANCE

            var approvedAttendanceDays =
                attendanceRecords.Count(x =>
                    x.Status == "Approved" &&
                    x.AttendanceApproval != null &&
                    x.AttendanceApproval.IsApproved);

            // APPROVED LEAVE

            var leaveDays =
                attendanceRecords.Count(x =>
                    x.Status == "Rejected" &&
                    x.AttendanceApproval != null &&
                    !x.AttendanceApproval.IsApproved &&
                    x.AttendanceApproval.Reason ==
                        DataEnums.AttendanceRejectionReason.Leave);

            // APPROVED SICK LEAVE

            var sickLeaveDays =
                attendanceRecords.Count(x =>
                    x.Status == "Rejected" &&
                    x.AttendanceApproval != null &&
                    !x.AttendanceApproval.IsApproved &&
                    x.AttendanceApproval.Reason ==
                        DataEnums.AttendanceRejectionReason.SickLeave);

            // APPROVED FAMILY RESPONSIBILITY LEAVE

            var familyResponsibilityLeaveDays =
                attendanceRecords.Count(x =>
                    x.Status == "Rejected" &&
                    x.AttendanceApproval != null &&
                    !x.AttendanceApproval.IsApproved &&
                    x.AttendanceApproval.Reason ==
                        DataEnums.AttendanceRejectionReason
                            .FamilyResponsibilityLeave);

            // STUDENT ABSENT

            // StudentAbsent is intentionally NOT included
            // in the eligible payment days.

            // TOTAL ELIGIBLE DAYS

            var totalEligibleDays =
                approvedAttendanceDays +
                leaveDays +
                sickLeaveDays +
                familyResponsibilityLeaveDays;

            // STIPEND CALCULATION

            var stipendAmount =
                totalEligibleDays * dailyRate;

            // NO PAYABLE DAYS

            if (totalEligibleDays == 0)
            {
                return null;
            }

            // CREATE PAYMENT SNAPSHOT

            return new StipendPayment
            {
                PaymentRunId =
                    paymentRunId,

                StudentId =
                    student.Id,

                EligibleAttendanceDays =
                    approvedAttendanceDays,

                LeaveDays =
                    leaveDays,

                SickLeaveDays =
                    sickLeaveDays,

                FamilyResponsibilityLeaveDays =
                    familyResponsibilityLeaveDays,

                TotalEligibleDays =
                    totalEligibleDays,

                DailyRate =
                    dailyRate,

                StipendAmount =
                    stipendAmount,

                // BANKING SNAPSHOT

                BankName =
                    student.Bank?.BankName,

                BranchCode =
                    student.BankBranch?.BranchCode,

                AccountNumber =
                    student.AccountNumber,

                AccountType =
                    student.AccountType?.AccountTypeName,

                AccountHolderName =
                    student.AccountHolderName,

                // PAYMENT STATUS
                Status = "Pending"
            };
        }
    }
}