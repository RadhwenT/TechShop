using Microsoft.Data.SqlClient;


namespace PROJET.Models.DAO
{
    public class MarqueDAO
    {
        private string chainecnx;
        public MarqueDAO(IConfiguration configuration)
        {
            chainecnx = configuration.GetConnectionString("cnx");
        }

        public List<Marque> GetAll()
        {
            List<Marque> marques = new List<Marque>();
            SqlConnection cnx = new SqlConnection(chainecnx);

            try
            {
                cnx.Open();
                SqlCommand cmd = new SqlCommand("SELECT Id, Name FROM Marques WHERE IsActive = 1", cnx);
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    Marque marque = new Marque()
                    {
                        Id = dr.GetFieldValue<int>(0),
                        Name = dr.GetFieldValue<string>(1)

                    };
                    marques.Add(marque);

                }

            }
            catch (Exception ex) { throw; }
            finally { cnx.Close(); }
            return marques;

        }

        public List<Marque> GetByName(string name)
        {
            List<Marque> marques = new List<Marque>();
            SqlConnection cnx = new SqlConnection(chainecnx);

            try
            {
                cnx.Open();
                SqlCommand cmd = new SqlCommand("SELECT Id, Name FROM Marques WHERE IsActive = 1 AND Name LIKE @Name", cnx);
                cmd.Parameters.AddWithValue("@Name", name + "%");
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    Marque marque = new Marque()
                    {
                        Id = dr.GetFieldValue<int>(0),
                        Name = dr.GetFieldValue<string>(1)

                    };
                    marques.Add(marque);

                }

            }
            catch (Exception ex) { throw; }
            finally { cnx.Close(); }
            return marques;

        }

        public bool AddMarque(Marque marque)
        {
            SqlConnection cnx = new SqlConnection(chainecnx);
            int n = 0;
            try
            {
                cnx.Open();
                SqlCommand cmd = new SqlCommand("INSERT INTO Marques (Name) VALUES (@Name)", cnx);
                cmd.Parameters.AddWithValue("@Name", marque.Name);
                n = cmd.ExecuteNonQuery();
            }
            catch (Exception ex) { throw; }
            finally { cnx.Close(); }
            if (n > 0) return true;
            return false;

        }

        public bool UpdateMarque(Marque marque)
        {
            SqlConnection cnx = new SqlConnection(chainecnx);
            int n = 0;
            try
            {
                cnx.Open();
                SqlCommand cmd = new SqlCommand("UPDATE Marques SET Name = @Name where Id = @Id", cnx);
                cmd.Parameters.AddWithValue("@Id", marque.Id);
                cmd.Parameters.AddWithValue("@Name", marque.Name);
                n = cmd.ExecuteNonQuery();
            }
            catch (Exception ex) { throw; }
            finally { cnx.Close(); }
            if (n > 0) return true;
            return false;

        }

        public bool DeleteMarque(int id)
        {
            SqlConnection cnx = new SqlConnection(chainecnx);
            int n = 0;
            try
            {
                cnx.Open();
                SqlCommand cmd = new SqlCommand("UPDATE Marques SET IsActive = 0 WHERE Id = @Id", cnx);
                cmd.Parameters.AddWithValue("@Id", id);

                n = cmd.ExecuteNonQuery();
            }
            catch (Exception ex) { throw; }
            finally { cnx.Close(); }
            if (n > 0) return true;
            return false;

        }

       
        public bool ExistsName(string name, int excludeId = 0)
        {
            SqlConnection cnx = new SqlConnection(chainecnx);
            try
            {
                cnx.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Marques WHERE LOWER(Name) = LOWER(@Name) AND Id <> @ExcludeId",
                    cnx);
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@ExcludeId", excludeId);
                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
            catch (Exception) { throw; }
            finally { cnx.Close(); }
        }

        
        public List<Marque> GetDeleted(string name = "")
        {
            List<Marque> marques = new List<Marque>();
            SqlConnection cnx = new SqlConnection(chainecnx);
            try
            {
                cnx.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT Id, Name FROM Marques WHERE IsActive = 0 AND Name LIKE @Name",
                    cnx);
                cmd.Parameters.AddWithValue("@Name", name + "%");
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    marques.Add(new Marque
                    {
                        Id = dr.GetFieldValue<int>(0),
                        Name = dr.GetFieldValue<string>(1)
                    });
                }
            }
            catch (Exception) { throw; }
            finally { cnx.Close(); }
            return marques;
        }

        
        public bool RestoreMarque(int id)
        {
            SqlConnection cnx = new SqlConnection(chainecnx);
            int n = 0;
            try
            {
                cnx.Open();
                SqlCommand cmd = new SqlCommand(
                    "UPDATE Marques SET IsActive = 1 WHERE Id = @Id",
                    cnx);
                cmd.Parameters.AddWithValue("@Id", id);
                n = cmd.ExecuteNonQuery();
            }
            catch (Exception) { throw; }
            finally { cnx.Close(); }
            return n > 0;
        }
    }
}