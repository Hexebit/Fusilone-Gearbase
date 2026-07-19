using Microsoft.Data.Sqlite;
using Fusilone.Helpers;

namespace Fusilone.Data;

public class DatabaseHelper
{
    private readonly string _connectionString;

    public DatabaseHelper()
    {
        _connectionString = $"Data Source={PathHelper.GetDatabasePath()}";
    }

    public SqliteConnection GetConnection()
    {
        return new SqliteConnection(_connectionString);
    }

    // Kolon zaten varsa SQLite hata verir; göç sırasında bu beklenen durumdur ve yutulur.
    private static void AddColumnIfMissing(SqliteConnection connection, string table, string columnDefinition)
    {
        try
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {columnDefinition}";
            cmd.ExecuteNonQuery();
        }
        catch { /* Kolon zaten mevcut */ }
    }

    // --- Reader → Model eşleyicileri (tekrarı önlemek için tek noktada) ---

    // "SELECT * FROM Devices" kolon sırasına göre bir Device okur.
    private static Models.Device MapDevice(SqliteDataReader reader)
    {
        return new Models.Device
        {
            Id = reader.GetString(0),
            Type = reader.GetString(1),
            Brand = reader.GetString(2),
            Model = reader.GetString(3),
            SerialNumber = reader.GetString(4),
            TechSpecs = reader.GetString(5),
            LastMaintenanceDate = DateTime.Parse(reader.GetString(6)),
            MaintenancePeriodMonths = reader.GetInt32(7),
            NextMaintenanceDate = DateTime.Parse(reader.GetString(8)),
            Cost = (decimal)reader.GetDouble(9),
            Status = reader.IsDBNull(10) ? "Aktif" : reader.GetString(10),
            PurchaseDate = reader.FieldCount > 11 && !reader.IsDBNull(11) ? DateTime.Parse(reader.GetString(11)) : DateTime.MinValue,
            ImageUrl = reader.FieldCount > 12 && !reader.IsDBNull(12) ? reader.GetString(12) : "",
            DeviceName = reader.FieldCount > 13 && !reader.IsDBNull(13) ? reader.GetString(13) : "",
            OwnerName = reader.FieldCount > 14 && !reader.IsDBNull(14) ? reader.GetString(14) : "",
            OwnerCustomerId = reader.FieldCount > 15 && !reader.IsDBNull(15) ? reader.GetInt32(15) : null,
            Notes = reader.FieldCount > 16 && !reader.IsDBNull(16) ? reader.GetString(16) : "",
            ManufactureDate = reader.FieldCount > 17 && !reader.IsDBNull(17) ? DateTime.Parse(reader.GetString(17)) : DateTime.MinValue,
            CreatedDate = reader.FieldCount > 18 && !reader.IsDBNull(18) ? DateTime.Parse(reader.GetString(18)) : DateTime.MinValue,
            WarrantyPeriodMonths = reader.FieldCount > 19 && !reader.IsDBNull(19) ? reader.GetInt32(19) : 24
        };
    }

    // "SELECT Id, Name, Email, Phone, CreatedAt FROM Customers" kolon sırasına göre bir Customer okur.
    private static Models.Customer MapCustomer(SqliteDataReader reader)
    {
        return new Models.Customer
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Email = reader.IsDBNull(2) ? "" : reader.GetString(2),
            Phone = reader.IsDBNull(3) ? "" : reader.GetString(3),
            CreatedAt = reader.IsDBNull(4) ? DateTime.Now : DateTime.Parse(reader.GetString(4))
        };
    }

    public void InitializeDatabase()
    {
        using var connection = GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = 
        @"
            CREATE TABLE IF NOT EXISTS Devices (
                Id TEXT PRIMARY KEY,
                Type TEXT,
                Brand TEXT,
                Model TEXT,
                SerialNumber TEXT,
                TechSpecs TEXT
            );
        ";
        command.ExecuteNonQuery();

        // Şema göçü: eksik kolonları ekle (var olanlar sessizce atlanır)
        string[] columnMigrations =
        {
            "LastMaintenanceDate TEXT DEFAULT '0001-01-01T00:00:00'",
            "MaintenancePeriodMonths INTEGER DEFAULT 6",
            "NextMaintenanceDate TEXT DEFAULT '0001-01-01T00:00:00'",
            "Cost REAL DEFAULT 0",
            "Status TEXT DEFAULT 'Aktif'",
            "PurchaseDate TEXT DEFAULT '0001-01-01T00:00:00'",
            "ImageUrl TEXT DEFAULT ''",
            "DeviceName TEXT DEFAULT ''",
            "OwnerName TEXT DEFAULT ''",
            "OwnerCustomerId INTEGER",
            "Notes TEXT DEFAULT ''",
            "ManufactureDate TEXT DEFAULT '0001-01-01T00:00:00'",
            "CreatedDate TEXT DEFAULT '0001-01-01T00:00:00'",
            "WarrantyPeriodMonths INTEGER DEFAULT 24"
        };

        foreach (var columnDef in columnMigrations)
        {
            AddColumnIfMissing(connection, "Devices", columnDef);
        }

        // Create Customers table
        var customerTableCmd = connection.CreateCommand();
        customerTableCmd.CommandText =
        @"
            CREATE TABLE IF NOT EXISTS Customers (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE,
                Email TEXT,
                Phone TEXT,
                CreatedAt TEXT
            );
        ";
        customerTableCmd.ExecuteNonQuery();

        // Create MaintenanceRecords table
        var recordTableCmd = connection.CreateCommand();
        recordTableCmd.CommandText = 
        @"
            CREATE TABLE IF NOT EXISTS MaintenanceRecords (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                DeviceId TEXT,
                Date TEXT,
                Description TEXT,
                Cost REAL,
                Notes TEXT,
                FOREIGN KEY(DeviceId) REFERENCES Devices(Id)
            );
        ";
        recordTableCmd.ExecuteNonQuery();

        // Create DevicePhotos table
        var photoTableCmd = connection.CreateCommand();
        photoTableCmd.CommandText = 
        @"
            CREATE TABLE IF NOT EXISTS DevicePhotos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                DeviceId TEXT,
                FilePath TEXT,
                Description TEXT,
                FOREIGN KEY(DeviceId) REFERENCES Devices(Id)
            );
        ";
        photoTableCmd.ExecuteNonQuery();

        // Create DeviceParts table
        var partsTableCmd = connection.CreateCommand();
        partsTableCmd.CommandText =
        @"
            CREATE TABLE IF NOT EXISTS DeviceParts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                DeviceId TEXT,
                PartName TEXT,
                PartNumber TEXT,
                Quantity INTEGER,
                Status TEXT,
                Notes TEXT,
                AddedAt TEXT,
                FOREIGN KEY(DeviceId) REFERENCES Devices(Id)
            );
        ";
        partsTableCmd.ExecuteNonQuery();

        // Create PartMovements table
        var movementTableCmd = connection.CreateCommand();
        movementTableCmd.CommandText =
        @"
            CREATE TABLE IF NOT EXISTS PartMovements (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                DeviceId TEXT,
                PartName TEXT,
                Action TEXT,
                Quantity INTEGER,
                Cost REAL,
                Notes TEXT,
                Date TEXT,
                FOREIGN KEY(DeviceId) REFERENCES Devices(Id)
            );
        ";
        movementTableCmd.ExecuteNonQuery();
    }

    public void AddDevicePhoto(Models.DevicePhoto photo)
    {
        using var connection = GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = 
        @"
            INSERT INTO DevicePhotos (DeviceId, FilePath, Description)
            VALUES ($deviceId, $filePath, $description);
        ";
        command.Parameters.AddWithValue("$deviceId", photo.DeviceId);
        command.Parameters.AddWithValue("$filePath", photo.FilePath);
        command.Parameters.AddWithValue("$description", photo.Description);

        command.ExecuteNonQuery();
    }

    public List<Models.DevicePhoto> GetPhotosByDeviceId(string deviceId)
    {
        var photos = new List<Models.DevicePhoto>();
        using var connection = GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM DevicePhotos WHERE DeviceId = $deviceId";
        command.Parameters.AddWithValue("$deviceId", deviceId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            photos.Add(new Models.DevicePhoto
            {
                Id = reader.GetInt32(0),
                DeviceId = reader.GetString(1),
                FilePath = reader.GetString(2),
                Description = reader.GetString(3)
            });
        }
        return photos;
    }

    public void AddMaintenanceRecord(Models.MaintenanceRecord record)
    {
        using var connection = GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = 
        @"
            INSERT INTO MaintenanceRecords (DeviceId, Date, Description, Cost, Notes)
            VALUES ($deviceId, $date, $description, $cost, $notes);
        ";
        command.Parameters.AddWithValue("$deviceId", record.DeviceId);
        command.Parameters.AddWithValue("$date", record.Date.ToString("o"));
        command.Parameters.AddWithValue("$description", record.Description);
        command.Parameters.AddWithValue("$cost", (double)record.Cost);
        command.Parameters.AddWithValue("$notes", record.Notes);

        command.ExecuteNonQuery();
    }

    public List<Models.MaintenanceRecord> GetRecordsByDeviceId(string deviceId)
    {
        var records = new List<Models.MaintenanceRecord>();
        using var connection = GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM MaintenanceRecords WHERE DeviceId = $deviceId ORDER BY Date DESC";
        command.Parameters.AddWithValue("$deviceId", deviceId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            records.Add(new Models.MaintenanceRecord
            {
                Id = reader.GetInt32(0),
                DeviceId = reader.GetString(1),
                Date = DateTime.Parse(reader.GetString(2)),
                Description = reader.GetString(3),
                Cost = (decimal)reader.GetDouble(4),
                Notes = reader.GetString(5)
            });
        }
        return records;
    }

    public void AddDevice(Models.Device device)
    {
        using var connection = GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = 
        @"
            INSERT INTO Devices (Id, Type, Brand, Model, SerialNumber, TechSpecs, LastMaintenanceDate, MaintenancePeriodMonths, NextMaintenanceDate, Cost, Status, PurchaseDate, ImageUrl, DeviceName, OwnerName, OwnerCustomerId, Notes, ManufactureDate, CreatedDate, WarrantyPeriodMonths)
            VALUES ($id, $type, $brand, $model, $serialNumber, $techSpecs, $lastMaint, $period, $nextMaint, $cost, $status, $purchaseDate, $imageUrl, $deviceName, $ownerName, $ownerCustomerId, $notes, $manufactureDate, $createdDate, $warrantyPeriodMonths);
        ";
        command.Parameters.AddWithValue("$id", device.Id);
        command.Parameters.AddWithValue("$type", device.Type);
        command.Parameters.AddWithValue("$brand", device.Brand);
        command.Parameters.AddWithValue("$model", device.Model);
        command.Parameters.AddWithValue("$serialNumber", device.SerialNumber);
        command.Parameters.AddWithValue("$techSpecs", device.TechSpecs);
        command.Parameters.AddWithValue("$lastMaint", device.LastMaintenanceDate.ToString("o"));
        command.Parameters.AddWithValue("$period", device.MaintenancePeriodMonths);
        command.Parameters.AddWithValue("$nextMaint", device.NextMaintenanceDate.ToString("o"));
        command.Parameters.AddWithValue("$cost", device.Cost);
        command.Parameters.AddWithValue("$status", device.Status);
        command.Parameters.AddWithValue("$purchaseDate", device.PurchaseDate.ToString("o"));
        command.Parameters.AddWithValue("$imageUrl", device.ImageUrl);
        command.Parameters.AddWithValue("$deviceName", device.DeviceName);
        command.Parameters.AddWithValue("$ownerName", device.OwnerName);
        command.Parameters.AddWithValue("$ownerCustomerId", (object?)device.OwnerCustomerId ?? DBNull.Value);
        command.Parameters.AddWithValue("$notes", device.Notes ?? "");
        command.Parameters.AddWithValue("$manufactureDate", device.ManufactureDate.ToString("o"));
        command.Parameters.AddWithValue("$createdDate", device.CreatedDate.ToString("o"));
        command.Parameters.AddWithValue("$warrantyPeriodMonths", device.WarrantyPeriodMonths);

        command.ExecuteNonQuery();
    }

    public void UpdateDevice(Models.Device device)
    {
        using var connection = GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = 
        @"
            UPDATE Devices 
            SET Type = $type, 
                Brand = $brand, 
                Model = $model, 
                SerialNumber = $serialNumber, 
                TechSpecs = $techSpecs, 
                LastMaintenanceDate = $lastMaint, 
                MaintenancePeriodMonths = $period, 
                NextMaintenanceDate = $nextMaint, 
                Cost = $cost, 
                Status = $status, 
                PurchaseDate = $purchaseDate, 
                ImageUrl = $imageUrl,
                DeviceName = $deviceName,
                OwnerName = $ownerName,
                OwnerCustomerId = $ownerCustomerId,
                Notes = $notes,
                ManufactureDate = $manufactureDate,
                WarrantyPeriodMonths = $warrantyPeriodMonths
            WHERE Id = $id;
        ";
        command.Parameters.AddWithValue("$id", device.Id);
        command.Parameters.AddWithValue("$type", device.Type);
        command.Parameters.AddWithValue("$brand", device.Brand);
        command.Parameters.AddWithValue("$model", device.Model);
        command.Parameters.AddWithValue("$serialNumber", device.SerialNumber);
        command.Parameters.AddWithValue("$techSpecs", device.TechSpecs);
        command.Parameters.AddWithValue("$lastMaint", device.LastMaintenanceDate.ToString("o"));
        command.Parameters.AddWithValue("$period", device.MaintenancePeriodMonths);
        command.Parameters.AddWithValue("$nextMaint", device.NextMaintenanceDate.ToString("o"));
        command.Parameters.AddWithValue("$cost", device.Cost);
        command.Parameters.AddWithValue("$status", device.Status);
        command.Parameters.AddWithValue("$purchaseDate", device.PurchaseDate.ToString("o"));
        command.Parameters.AddWithValue("$imageUrl", device.ImageUrl);
        command.Parameters.AddWithValue("$deviceName", device.DeviceName);
        command.Parameters.AddWithValue("$ownerName", device.OwnerName);
        command.Parameters.AddWithValue("$ownerCustomerId", (object?)device.OwnerCustomerId ?? DBNull.Value);
        command.Parameters.AddWithValue("$notes", device.Notes ?? "");
        command.Parameters.AddWithValue("$manufactureDate", device.ManufactureDate.ToString("o"));
        command.Parameters.AddWithValue("$warrantyPeriodMonths", device.WarrantyPeriodMonths);

        command.ExecuteNonQuery();
    }

    public int GetNextSequenceNumber(string typeCode)
    {
        using var connection = GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        // ID formatımız FSL-{TYPE}-{NUMBER} (Örn: FSL-PC-001)
        // Bu yüzden ilgili tipteki son kaydı bulup numarasını alacağız.
        command.CommandText = 
        @"
            SELECT Id FROM Devices 
            WHERE Type = $type 
            ORDER BY Id DESC 
            LIMIT 1;
        ";
        command.Parameters.AddWithValue("$type", typeCode);

        var result = command.ExecuteScalar();

        if (result == null || result == DBNull.Value)
        {
            return 1;
        }

        string lastId = result.ToString()!;
        // ID formatını parçala: FSL-PC-005 -> ["FSL", "PC", "005"]
        var parts = lastId.Split('-');
        if (parts.Length > 2 && int.TryParse(parts[parts.Length - 1], out int lastNumber))
        {
            return lastNumber + 1;
        }

        return 1;
    }

    public List<Models.Device> GetAllDevices()
    {
        var devices = new List<Models.Device>();
        using var connection = GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Devices";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            devices.Add(MapDevice(reader));
        }

        return devices;
    }

    public Models.Customer? GetCustomerByName(string name)
    {
        using var connection = GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Email, Phone, CreatedAt FROM Customers WHERE Name = $name";
        command.Parameters.AddWithValue("$name", name);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return MapCustomer(reader);
        }

        return null;
    }

    public List<Models.Customer> GetAllCustomers()
    {
        var list = new List<Models.Customer>();
        using var connection = GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Email, Phone, CreatedAt FROM Customers ORDER BY Name";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapCustomer(reader));
        }

        return list;
    }

    public List<Models.Customer> SearchCustomersByName(string prefix)
    {
        var list = new List<Models.Customer>();
        using var connection = GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Email, Phone, CreatedAt FROM Customers WHERE Name LIKE $name ORDER BY Name LIMIT 10";
        command.Parameters.AddWithValue("$name", $"%{prefix}%");

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapCustomer(reader));
        }

        return list;
    }

    public Models.Customer UpsertCustomer(string name, string email, string phone)
    {
        using var connection = GetConnection();
        connection.Open();

        var existing = GetCustomerByName(name);
        if (existing != null)
        {
            var update = connection.CreateCommand();
            update.CommandText = "UPDATE Customers SET Email = $email, Phone = $phone WHERE Id = $id";
            update.Parameters.AddWithValue("$id", existing.Id);
            update.Parameters.AddWithValue("$email", email ?? "");
            update.Parameters.AddWithValue("$phone", phone ?? "");
            update.ExecuteNonQuery();

            existing.Email = email ?? "";
            existing.Phone = phone ?? "";
            return existing;
        }

        var insert = connection.CreateCommand();
        insert.CommandText = "INSERT INTO Customers (Name, Email, Phone, CreatedAt) VALUES ($name, $email, $phone, $createdAt);";
        insert.Parameters.AddWithValue("$name", name);
        insert.Parameters.AddWithValue("$email", email ?? "");
        insert.Parameters.AddWithValue("$phone", phone ?? "");
        insert.Parameters.AddWithValue("$createdAt", DateTime.Now.ToString("o"));
        insert.ExecuteNonQuery();

        return GetCustomerByName(name) ?? new Models.Customer { Name = name, Email = email ?? "", Phone = phone ?? "" };
    }

    public (int TotalDevices, decimal TotalCost) GetCustomerStats(int customerId)
    {
        using var connection = GetConnection();
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*), IFNULL(SUM(Cost), 0) FROM Devices WHERE OwnerCustomerId = $id";
        cmd.Parameters.AddWithValue("$id", customerId);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            int count = reader.GetInt32(0);
            decimal total = (decimal)reader.GetDouble(1);
            return (count, total);
        }

        return (0, 0);
    }

    public List<Models.Device> GetDevicesByCustomerId(int customerId)
    {
        var devices = new List<Models.Device>();
        using var connection = GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Devices WHERE OwnerCustomerId = $id ORDER BY Brand, Model";
        command.Parameters.AddWithValue("$id", customerId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            devices.Add(MapDevice(reader));
        }

        return devices;
    }

    public Models.DevicePart UpsertDevicePart(string deviceId, string partName, string partNumber, int quantity, string status, string notes)
    {
        using var connection = GetConnection();
        connection.Open();

        var find = connection.CreateCommand();
        find.CommandText = "SELECT Id, Quantity FROM DeviceParts WHERE DeviceId = $deviceId AND PartName = $partName AND IFNULL(PartNumber,'') = $partNumber";
        find.Parameters.AddWithValue("$deviceId", deviceId);
        find.Parameters.AddWithValue("$partName", partName);
        find.Parameters.AddWithValue("$partNumber", partNumber ?? "");

        int? existingId = null;
        int existingQty = 0;

        using (var reader = find.ExecuteReader())
        {
            if (reader.Read())
            {
                existingId = reader.GetInt32(0);
                existingQty = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            }
        }

        if (existingId.HasValue)
        {
            var update = connection.CreateCommand();
            update.CommandText = "UPDATE DeviceParts SET Quantity = $qty, Status = $status, Notes = $notes WHERE Id = $id";
            update.Parameters.AddWithValue("$id", existingId.Value);
            update.Parameters.AddWithValue("$qty", existingQty + Math.Max(0, quantity));
            update.Parameters.AddWithValue("$status", status ?? "");
            update.Parameters.AddWithValue("$notes", notes ?? "");
            update.ExecuteNonQuery();

            return new Models.DevicePart
            {
                Id = existingId.Value,
                DeviceId = deviceId,
                PartName = partName,
                PartNumber = partNumber ?? "",
                Quantity = existingQty + Math.Max(0, quantity),
                Status = status ?? "",
                Notes = notes ?? "",
                AddedAt = DateTime.Now
            };
        }

        var insert = connection.CreateCommand();
        insert.CommandText = "INSERT INTO DeviceParts (DeviceId, PartName, PartNumber, Quantity, Status, Notes, AddedAt) VALUES ($deviceId, $partName, $partNumber, $quantity, $status, $notes, $addedAt);";
        insert.Parameters.AddWithValue("$deviceId", deviceId);
        insert.Parameters.AddWithValue("$partName", partName);
        insert.Parameters.AddWithValue("$partNumber", partNumber ?? "");
        insert.Parameters.AddWithValue("$quantity", Math.Max(0, quantity));
        insert.Parameters.AddWithValue("$status", status ?? "");
        insert.Parameters.AddWithValue("$notes", notes ?? "");
        insert.Parameters.AddWithValue("$addedAt", DateTime.Now.ToString("o"));
        insert.ExecuteNonQuery();

        var idCmd = connection.CreateCommand();
        idCmd.CommandText = "SELECT last_insert_rowid();";
        int id = Convert.ToInt32(idCmd.ExecuteScalar());

        return new Models.DevicePart
        {
            Id = id,
            DeviceId = deviceId,
            PartName = partName,
            PartNumber = partNumber ?? "",
            Quantity = Math.Max(0, quantity),
            Status = status ?? "",
            Notes = notes ?? "",
            AddedAt = DateTime.Now
        };
    }

    public List<Models.DevicePart> GetDevicePartsByDeviceId(string deviceId)
    {
        var list = new List<Models.DevicePart>();
        using var connection = GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, DeviceId, PartName, PartNumber, Quantity, Status, Notes, AddedAt FROM DeviceParts WHERE DeviceId = $deviceId ORDER BY PartName";
        command.Parameters.AddWithValue("$deviceId", deviceId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Models.DevicePart
            {
                Id = reader.GetInt32(0),
                DeviceId = reader.GetString(1),
                PartName = reader.GetString(2),
                PartNumber = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Quantity = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                Status = reader.IsDBNull(5) ? "" : reader.GetString(5),
                Notes = reader.IsDBNull(6) ? "" : reader.GetString(6),
                AddedAt = reader.IsDBNull(7) ? DateTime.Now : DateTime.Parse(reader.GetString(7))
            });
        }

        return list;
    }

    public void UpdateDevicePartQuantity(int partId, int quantity)
    {
        using var connection = GetConnection();
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE DeviceParts SET Quantity = $qty WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", partId);
        cmd.Parameters.AddWithValue("$qty", Math.Max(0, quantity));
        cmd.ExecuteNonQuery();
    }

    public void AddPartMovement(Models.PartMovement movement)
    {
        using var connection = GetConnection();
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO PartMovements (DeviceId, PartName, Action, Quantity, Cost, Notes, Date) VALUES ($deviceId, $partName, $action, $quantity, $cost, $notes, $date);";
        cmd.Parameters.AddWithValue("$deviceId", movement.DeviceId);
        cmd.Parameters.AddWithValue("$partName", movement.PartName);
        cmd.Parameters.AddWithValue("$action", movement.Action);
        cmd.Parameters.AddWithValue("$quantity", movement.Quantity);
        cmd.Parameters.AddWithValue("$cost", movement.Cost);
        cmd.Parameters.AddWithValue("$notes", movement.Notes);
        cmd.Parameters.AddWithValue("$date", movement.Date.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public List<Models.PartMovement> GetPartMovementsByDeviceId(string deviceId)
    {
        var list = new List<Models.PartMovement>();
        using var connection = GetConnection();
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, DeviceId, PartName, Action, Quantity, Cost, Notes, Date FROM PartMovements WHERE DeviceId = $deviceId ORDER BY Date DESC";
        cmd.Parameters.AddWithValue("$deviceId", deviceId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Models.PartMovement
            {
                Id = reader.GetInt32(0),
                DeviceId = reader.GetString(1),
                PartName = reader.GetString(2),
                Action = reader.GetString(3),
                Quantity = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                Cost = reader.IsDBNull(5) ? 0 : (decimal)reader.GetDouble(5),
                Notes = reader.IsDBNull(6) ? "" : reader.GetString(6),
                Date = reader.IsDBNull(7) ? DateTime.Now : DateTime.Parse(reader.GetString(7))
            });
        }

        return list;
    }

    public void DeleteDevice(string deviceId)
    {
        using var connection = GetConnection();
        connection.Open();

        using var tran = connection.BeginTransaction();
        try
        {
            // ensure foreign keys enforced
            using var fkCmd = connection.CreateCommand();
            fkCmd.CommandText = "PRAGMA foreign_keys = ON;";
            fkCmd.ExecuteNonQuery();

            using var delRecords = connection.CreateCommand();
            delRecords.CommandText = "DELETE FROM MaintenanceRecords WHERE DeviceId = $id;";
            delRecords.Parameters.AddWithValue("$id", deviceId);
            delRecords.ExecuteNonQuery();

            using var delPhotos = connection.CreateCommand();
            delPhotos.CommandText = "DELETE FROM DevicePhotos WHERE DeviceId = $id;";
            delPhotos.Parameters.AddWithValue("$id", deviceId);
            delPhotos.ExecuteNonQuery();

            using var delParts = connection.CreateCommand();
            delParts.CommandText = "DELETE FROM DeviceParts WHERE DeviceId = $id;";
            delParts.Parameters.AddWithValue("$id", deviceId);
            delParts.ExecuteNonQuery();

            using var delMovements = connection.CreateCommand();
            delMovements.CommandText = "DELETE FROM PartMovements WHERE DeviceId = $id;";
            delMovements.Parameters.AddWithValue("$id", deviceId);
            delMovements.ExecuteNonQuery();

            using var delDevice = connection.CreateCommand();
            delDevice.CommandText = "DELETE FROM Devices WHERE Id = $id;";
            delDevice.Parameters.AddWithValue("$id", deviceId);
            delDevice.ExecuteNonQuery();

            tran.Commit();
        }
        catch
        {
            tran.Rollback();
            throw;
        }
    }
}
