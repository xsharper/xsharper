<?php

function getmicrotime(){
  list($usec, $sec) = explode(" ",microtime());
  return ((float)$usec + (float)$sec);
}

	$time_start = getmicrotime();
	$v_t='T';

 for ($i=0;$i<200000;++$i)
	  $x=( ( $v_t == 'B' ) ? strlen('bus') : 
			( $v_t == 'A' ) ? strlen('airplane')+10 : 
			( $v_t == 'T' ) ? strlen('train')+100 : 
			( $v_t == 'C' ) ? strlen('car') +1000:  
			( $v_t == 'H' ) ? strlen('horse') +10000:  
			strlen('feet')+100000 );

	$eval=getmicrotime() - $time_start;
	echo 'Evaluation time: '. round($eval,4) .' seconds.';
	echo '\n';

	$time_start = getmicrotime();
	for ($i=0;$i<200000;++$i)
		eval("( ( $v_t == 'B' ) ? strlen('bus') : ( $v_t == 'A' ) ? strlen('airplane')+10 : 	( $v_t == 'T' ) ? strlen('train')+100 : 	( $v_t == 'C' ) ? strlen('car') +1000:  	( $v_t == 'H' ) ? strlen('horse') +10000:  	strlen('feet')+100000 );");

	echo 'Parsing time: '. round(getmicrotime() - $time_start-$eval,4) .' seconds.';
	echo '\n';

	

?>