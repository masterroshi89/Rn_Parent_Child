<?

/**
 * CPMObjectEventHandler: parentChildUpdation
 * Objects: Incident
 * Actions: Update
 * Version: 1.3
 */

use \RightNow\Connect\v1_3 as Connect;
use \RightNow\CPM\v1 as RNCPM;


class parentChildUpdation implements RNCPM\ObjectEventHandler
{


    public static function apply($run_mode, $action, $obj, $n_cycles)
    {
        if ($n_cycles !== 0)
            return;

        //$contactobj = $obj->PrimaryContact;
        try {

            $filters = new Connect\AnalyticsReportSearchFilterArray;
			$filter= new Connect\AnalyticsReportSearchFilter;
			$filter->Name = 'RefNumber';
			$filter->Values = array($obj->ID);
			$filters[0] = $filter;
			$ar= Connect\AnalyticsReport::fetch(100005);
			$arr=$ar->run( 0, $filters );
			$nrows= $arr->count();
			if ( $nrows) {
			$row = $arr->next();
				for ( $ii = 0; $ii++ < $nrows; $row = $arr->next() ) {
						echo $row['RelatedIncident']." \n";
						$incident = Connect\Incident::fetch( $row['RelatedIncident']);
						$incident->StatusWithType->Status->ID = 2 ;
						$incident->save();
						echo $incident->StatusWithType->Status->ID;
				}
			}          
        } catch (Connect\ConnectAPIError $err) {
            print($err);
        } catch (\Exception $err) {

            print($err);
        }
    }
}

class parentChildUpdation_TestHarness implements RNCPM\ObjectEventHandler_TestHarness
{
    static $obj_invented = NULL;
    static $obj = NULL;

    public static function setup()
    {

        //$query = "ID = 45147";
        $obj   = Connect\Incident::fetch(10);
        $obj->save();

        static::$obj_invented = $obj;
        return;
    }

    public static function fetchObject($action, $object_type)
    {

        return (static::$obj_invented);
    }

    public static function validate($action, $object)
    {
        return true;
    }

    public static function cleanup()
    {

        static::$obj_invented = NULL;
        return;
    }
}
