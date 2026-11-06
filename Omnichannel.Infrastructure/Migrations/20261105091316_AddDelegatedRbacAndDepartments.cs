using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Omnichannel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDelegatedRbacAndDepartments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Users",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "Users",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Users",
                type: "datetime2(7)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeCode",
                table: "Users",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HireDate",
                table: "Users",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "Users",
                type: "datetime2(7)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ManagerId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedByUserId",
                table: "UserRoles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GrantedAt",
                table: "UserPermissions",
                type: "datetime2(7)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "GrantedByUserId",
                table: "UserPermissions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "UserPermissions",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Roles",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "Roles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "Roles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystem",
                table: "Roles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "Roles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Roles",
                type: "datetime2(7)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdminDelegationScopes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminUserId = table.Column<int>(type: "int", nullable: false),
                    CanCreateUser = table.Column<bool>(type: "bit", nullable: false),
                    CanEditUser = table.Column<bool>(type: "bit", nullable: false),
                    CanDeleteUser = table.Column<bool>(type: "bit", nullable: false),
                    CanResetPassword = table.Column<bool>(type: "bit", nullable: false),
                    CanAssignRole = table.Column<bool>(type: "bit", nullable: false),
                    CanDelegateFurther = table.Column<bool>(type: "bit", nullable: false),
                    MaxGrantLevel = table.Column<int>(type: "int", nullable: false),
                    GrantedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminDelegationScopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminDelegationScopes_Users_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdminDelegationScopes_Users_GrantedByUserId",
                        column: x => x.GrantedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    HeadUserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departments_Departments_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Departments_Users_HeadUserId",
                        column: x => x.HeadUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AdminDelegationPermissions",
                columns: table => new
                {
                    AdminDelegationScopeId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminDelegationPermissions", x => new { x.AdminDelegationScopeId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_AdminDelegationPermissions_AdminDelegationScopes_AdminDelegationScopeId",
                        column: x => x.AdminDelegationScopeId,
                        principalTable: "AdminDelegationScopes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdminDelegationPermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdminDelegationStores",
                columns: table => new
                {
                    AdminDelegationScopeId = table.Column<int>(type: "int", nullable: false),
                    StoreId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminDelegationStores", x => new { x.AdminDelegationScopeId, x.StoreId });
                    table.ForeignKey(
                        name: "FK_AdminDelegationStores_AdminDelegationScopes_AdminDelegationScopeId",
                        column: x => x.AdminDelegationScopeId,
                        principalTable: "AdminDelegationScopes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdminDelegationStores_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdminDelegationDepartments",
                columns: table => new
                {
                    AdminDelegationScopeId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminDelegationDepartments", x => new { x.AdminDelegationScopeId, x.DepartmentId });
                    table.ForeignKey(
                        name: "FK_AdminDelegationDepartments_AdminDelegationScopes_AdminDelegationScopeId",
                        column: x => x.AdminDelegationScopeId,
                        principalTable: "AdminDelegationScopes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdminDelegationDepartments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_DepartmentId",
                table: "Users",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ManagerId",
                table: "Users",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "UQ_Users_EmployeeCode",
                table: "Users",
                column: "EmployeeCode",
                unique: true,
                filter: "[EmployeeCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_AssignedByUserId",
                table: "UserRoles",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_GrantedByUserId",
                table: "UserPermissions",
                column: "GrantedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_DepartmentId",
                table: "Roles",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminDelegationDepartments_DepartmentId",
                table: "AdminDelegationDepartments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminDelegationPermissions_PermissionId",
                table: "AdminDelegationPermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminDelegationScopes_GrantedByUserId",
                table: "AdminDelegationScopes",
                column: "GrantedByUserId");

            migrationBuilder.CreateIndex(
                name: "UQ_AdminDelegationScopes_AdminUserId",
                table: "AdminDelegationScopes",
                column: "AdminUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminDelegationStores_StoreId",
                table: "AdminDelegationStores",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Code",
                table: "Departments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_HeadUserId",
                table: "Departments",
                column: "HeadUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ParentId",
                table: "Departments",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Roles_Departments_DepartmentId",
                table: "Roles",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UserPermissions_Users_GrantedByUserId",
                table: "UserPermissions",
                column: "GrantedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Users_AssignedByUserId",
                table: "UserRoles",
                column: "AssignedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Departments_DepartmentId",
                table: "Users",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_ManagerId",
                table: "Users",
                column: "ManagerId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Roles_Departments_DepartmentId",
                table: "Roles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserPermissions_Users_GrantedByUserId",
                table: "UserPermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Users_AssignedByUserId",
                table: "UserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Departments_DepartmentId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_ManagerId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "AdminDelegationDepartments");

            migrationBuilder.DropTable(
                name: "AdminDelegationPermissions");

            migrationBuilder.DropTable(
                name: "AdminDelegationStores");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "AdminDelegationScopes");

            migrationBuilder.DropIndex(
                name: "IX_Users_DepartmentId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ManagerId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "UQ_Users_EmployeeCode",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_UserRoles_AssignedByUserId",
                table: "UserRoles");

            migrationBuilder.DropIndex(
                name: "IX_UserPermissions_GrantedByUserId",
                table: "UserPermissions");

            migrationBuilder.DropIndex(
                name: "IX_Roles_DepartmentId",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmployeeCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "HireDate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "JobTitle",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ManagerId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AssignedByUserId",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "GrantedAt",
                table: "UserPermissions");

            migrationBuilder.DropColumn(
                name: "GrantedByUserId",
                table: "UserPermissions");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "UserPermissions");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "IsSystem",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Roles");
        }
    }
}
