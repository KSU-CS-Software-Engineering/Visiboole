using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisiBoole.ParsingEngine
{
    class Temp
    {
        /* Inside IsScalar
        // If scalar doesn't contain a bit
        if (bit == -1)
        {
            // If namespace belongs to a vector
            if (Design.Database.NamespaceBelongsToVector(name))
            {
                // Add namespace error to error log
                ErrorLog.Add($"{LineNumber}: Namespace '{name}' is already being used by a vector.");
                return false;
            }
            // If namespace doesn't exist
            else if (!Design.Database.NamespaceExists(name))
            {
                // Update namespace with no bit
                Design.Database.UpdateNamespace(name, bit);
            }
        }
        // If scalar does contain a bit
        else
        {
            // If namespace exists and doesn't belong to a vector
            if (Design.Database.NamespaceExists(name) && !Design.Database.NamespaceBelongsToVector(name))
            {
                // Add namespace error to error log
                ErrorLog.Add($"{LineNumber}: Namespace '{name}' is already being used by a scalar.");
                return false;
            }
            // If namespace doesn't exist or belongs to a vector
            else
            {
                // Update/add namespace with bit
                Design.Database.UpdateNamespace(name, bit);
            }
        }
        */

        // If namespace exists and doesn't belong to a vector
        if (Design.Database.NamespaceExists(name) && !Design.Database.NamespaceBelongsToVector(name))
        {
            // Add namespace error to error log
            ErrorLog.Add($"{LineNumber}: Namespace '{name}' is already being used by a scalar.");
            return false;
        }

        /* In vector
        // If vector is explicit
        if (leftBound != -1)
        {
            // If left bound is least significant bit
            if (leftBound<rightBound)
            {
                // Flips bounds so left bound is most significant bit
                leftBound = leftBound + rightBound;
                rightBound = leftBound - rightBound;
                leftBound = leftBound - rightBound;
            }

            // For each bit in the vector bounds
            for (int i = leftBound; i >= rightBound; i--)
            {
                // Update/add bit to namespace
                Design.Database.UpdateNamespace(name, i);
            }
        }
        */
    }
}
