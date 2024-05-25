using FileUploadMVcProcedure.Context;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FileUploadMVcProcedure.Controllers
{
    public class EmployeeController : Controller
    {
        FileUploadMVcProcedureEntities _context = new FileUploadMVcProcedureEntities();

        // GET: Employee
        public ActionResult Index()
        {
            // Call the stored procedure to get the employee list
            var employeeList = _context.Database.SqlQuery<Employee>("EXEC GetEmployees").ToList();
            return View(employeeList);
        }

        // Create Employee
        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(Employee employee, HttpPostedFileBase file)
        {
            if (ModelState.IsValid)
            {
                // Check if employee already exists
                var existEmployee = _context.Employees.Any(e => e.EmployeeEmail == employee.EmployeeEmail);

                if (existEmployee)
                {
                    ModelState.AddModelError("EmployeeEmail", "Employee already registered");
                    return View(employee);
                }

                if (file != null && file.ContentLength > 0)
                {
                    // Generate a unique filename
                    var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                    var extension = Path.GetExtension(file.FileName);
                    var uniqueFileName = $"{fileName}_{Guid.NewGuid()}{extension}";

                    // Define the path to save the file
                    var path = Path.Combine(Server.MapPath("~/Uploads"), uniqueFileName);
                    file.SaveAs(path);

                    employee.Profile = $"~/Uploads/{uniqueFileName}";
                }

                employee.LastUpdate = DateTime.Now;

                // Call the stored procedure to insert employee using ExecuteSqlCommand directly
                _context.Database.ExecuteSqlCommand(
                    "EXEC InsertEmployee @EmployeeName, @EmployeeEmail, @Password, @Address, @Profile, @LastUpdate",
                    new SqlParameter("@EmployeeName", employee.EmployeeName),
                    new SqlParameter("@EmployeeEmail", employee.EmployeeEmail),
                    new SqlParameter("@Password", employee.Password),
                    new SqlParameter("@Address", (object)employee.Address ?? DBNull.Value),
                    new SqlParameter("@Profile", (object)employee.Profile ?? DBNull.Value),
                    new SqlParameter("@LastUpdate", employee.LastUpdate)
                );

                ModelState.Clear();
                return RedirectToAction("Index");
            }

            return View(employee);
        }

        //Employee Edit
        [HttpGet]
        public ActionResult Edit(int employeeId)
        {
            var existEmployee = _context.Employees.Find(employeeId);
            if (existEmployee == null)
            {
                return HttpNotFound();
            }

            return View(existEmployee);
        }

        [HttpPost]
        public ActionResult Edit(Employee employee, HttpPostedFileBase file)
        {
            if (ModelState.IsValid)
            {
                // Retrieve the existing employee from the database
                var existingEmployee = _context.Employees.Find(employee.EmployeeId);
                if (existingEmployee == null)
                {
                    return HttpNotFound();
                }

                // Handle file upload
                if (file != null && file.ContentLength > 0)
                {
                    // Delete the old profile image if a new one is uploaded
                    if (!string.IsNullOrEmpty(existingEmployee.Profile))
                    {
                        var oldFilePath = Server.MapPath(existingEmployee.Profile);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // Save the new file
                    var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                    var extension = Path.GetExtension(file.FileName);
                    var uniqueFileName = $"{fileName}_{Guid.NewGuid()}{extension}";
                    var path = Path.Combine(Server.MapPath("~/Uploads"), uniqueFileName);
                    file.SaveAs(path);

                    employee.Profile = $"~/Uploads/{uniqueFileName}";
                }
                else
                {
                    // If no new file is uploaded, preserve the existing profile path
                    employee.Profile = existingEmployee.Profile;
                }

                // Update employee properties
                existingEmployee.EmployeeName = employee.EmployeeName;
                existingEmployee.EmployeeEmail = employee.EmployeeEmail;
                existingEmployee.Password = employee.Password;
                existingEmployee.Address = employee.Address;
                existingEmployee.Profile = employee.Profile;
                existingEmployee.LastUpdate = DateTime.Now;

                // Call the stored procedure to update employee
                _context.Database.ExecuteSqlCommand(
                    "EXEC UpdateEmployee @EmployeeId, @EmployeeName, @EmployeeEmail, @Password, @Address, @Profile, @LastUpdate",
                    new SqlParameter("@EmployeeId", existingEmployee.EmployeeId),
                    new SqlParameter("@EmployeeName", existingEmployee.EmployeeName),
                    new SqlParameter("@EmployeeEmail", existingEmployee.EmployeeEmail),
                    new SqlParameter("@Password", existingEmployee.Password),
                    new SqlParameter("@Address", (object)existingEmployee.Address ?? DBNull.Value),
                    new SqlParameter("@Profile", (object)existingEmployee.Profile ?? DBNull.Value),
                    new SqlParameter("@LastUpdate", existingEmployee.LastUpdate)
                );

                return RedirectToAction("Index");
            }

            return View(employee);
        }

        // Delete Employee
        public ActionResult Delete(int employeeId)
        {
            var existEmployee = _context.Employees.Find(employeeId);
            if (existEmployee == null)
            {
                return HttpNotFound();
            }

            // Delete the profile image file if it exists
            if (!string.IsNullOrEmpty(existEmployee.Profile))
            {
                var filePath = Server.MapPath(existEmployee.Profile);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            // Call the stored procedure to delete the employee
            _context.Database.ExecuteSqlCommand(
                "EXEC DeleteEmployee @EmployeeId",
                new SqlParameter("@EmployeeId", employeeId)
            );

            var employeeList = _context.Employees.ToList();
            return View("Index", employeeList);
        }

    }
}